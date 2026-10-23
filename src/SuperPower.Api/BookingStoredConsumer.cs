using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace SuperPower.Api;

// Consumes "booking-stored" and adds one to the event's count (ADR-0030, ADR-0032).
public sealed class BookingStoredConsumer(IConnection connection, IConnectionMultiplexer redis, ILogger<BookingStoredConsumer> logger)
    : BackgroundService
{
    private const string Queue = "booking-stored";

    private static readonly Meter Meter = new("Acme.Bookings");
    private static readonly Counter<long> Counted = Meter.CreateCounter<long>("acme.bookings.counted");

    public static string CountKey(Guid eventKey) => $"event:{eventKey}:bookings";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Counted.Add(0); // series exists at 0, so the first booking after a start shows in increase() (ADR-0034)
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            Guid? eventKey = null;
            try
            {
                // Demo Code : Act 3
                // The site publishes "eventKey"; reading any other property name fails every booking message.
                // Fix: read "eventKey".
                // Initial Demo State - comment out the "eventKey" line, uncomment the "eventId" line
                eventKey = JsonDocument.Parse(ea.Body).RootElement.GetProperty("eventKey").GetGuid();
                // eventKey = JsonDocument.Parse(ea.Body).RootElement.GetProperty("eventId").GetGuid();
                Activity.Current?.SetTag("acme.event.key", eventKey);
                var count = await redis.GetDatabase().StringIncrementAsync(CountKey(eventKey.Value));
                Counted.Add(1);
                logger.LogInformation("Booking recorded for {EventKey}, count {Count}", eventKey, count);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Activity.Current?.AddException(ex);
                Activity.Current?.SetStatus(ActivityStatusCode.Error);
                logger.LogError(ex, "Booking update failed for {EventKey}", eventKey);
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(Queue, autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        await channel.DisposeAsync();
    }
}

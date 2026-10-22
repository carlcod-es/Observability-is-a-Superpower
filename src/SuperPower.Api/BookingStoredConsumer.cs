using System.Diagnostics;
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

    public static string CountKey(Guid eventKey) => $"event:{eventKey}:bookings";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            Guid? eventKey = null;
            try
            {
                eventKey = JsonDocument.Parse(ea.Body).RootElement.GetProperty("eventKey").GetGuid();
                Activity.Current?.SetTag("acme.event.key", eventKey);
                var count = await redis.GetDatabase().StringIncrementAsync(CountKey(eventKey.Value));
                logger.LogInformation("Booking recorded for {EventKey}, count {Count}", eventKey, count);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Booking update failed for {EventKey}", eventKey);
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(Queue, autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        await channel.DisposeAsync();
    }
}

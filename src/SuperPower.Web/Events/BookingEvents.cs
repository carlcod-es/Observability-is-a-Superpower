using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;
using SuperPower.Web.Forms;

namespace SuperPower.Web.Events;

/// <summary>Tells the booking service a booking was stored (ADR-0032). Never throws.</summary>
public interface IBookingEvents
{
    Task PublishStoredAsync(Guid eventKey, CancellationToken ct = default);
}

public sealed class RabbitBookingEvents(IServiceProvider services, ILogger<RabbitBookingEvents> logger) : IBookingEvents
{
    private const string Queue = "booking-stored";

    public async Task PublishStoredAsync(Guid eventKey, CancellationToken ct = default)
    {
        using var activity = AcmeFormsTelemetry.ActivitySource.StartActivity("acme.forms.publish");
        activity?.SetTag("acme.event.key", eventKey.ToString());
        try
        {
            // Task.Run: resolving the Aspire IConnection can block while connecting.
            await Task.Run(() => PublishAsync(eventKey), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2), ct);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            logger.LogWarning(ex, "Booking event not queued for {EventKey}", eventKey);
        }
    }

    private async Task PublishAsync(Guid eventKey)
    {
        var connection = services.GetRequiredService<IConnection>();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        var body = Encoding.UTF8.GetBytes($"{{\"eventKey\":\"{eventKey}\"}}");
        await channel.BasicPublishAsync("", Queue, body);
    }
}

/// <summary>Used when the site runs without the queue.</summary>
public sealed class NoBookingEvents : IBookingEvents
{
    public Task PublishStoredAsync(Guid eventKey, CancellationToken ct = default) => Task.CompletedTask;
}

using System.Diagnostics;
using SuperPower.Api;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("cache");
builder.AddRabbitMQClient("queue");
builder.Services.AddHostedService<BookingStoredConsumer>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Booking count for an event; 0 when there is no key (ADR-0030).
app.MapGet("/events/{eventKey:guid}/bookings", async (Guid eventKey, IConnectionMultiplexer redis) =>
{
    Activity.Current?.SetTag("acme.event.key", eventKey);
    var value = await redis.GetDatabase().StringGetAsync(BookingStoredConsumer.CountKey(eventKey));
    return new { count = value.HasValue ? (long)value : 0L };
});

app.Run();

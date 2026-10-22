using System.Net.Http.Json;

namespace SuperPower.Web.Events;

/// <summary>Booking count per event from the booking service (ADR-0030). Never throws; null = unavailable.</summary>
public interface IBookingCounts
{
    Task<int?> GetAsync(Guid eventKey, CancellationToken ct = default);
}

public sealed class BookingCountsClient(HttpClient http, ILogger<BookingCountsClient> logger) : IBookingCounts
{
    private sealed record CountResponse(int Count);

    public async Task<int?> GetAsync(Guid eventKey, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetFromJsonAsync<CountResponse>($"events/{eventKey}/bookings", ct);
            return response?.Count;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Booking service call failed for {EventKey}", eventKey);
            return null;
        }
    }
}

/// <summary>Used when the site runs without the booking service.</summary>
public sealed class NoBookingCounts : IBookingCounts
{
    public Task<int?> GetAsync(Guid eventKey, CancellationToken ct = default) => Task.FromResult<int?>(null);
}

public static class BookingCountText
{
    public static string? Format(int? count) => count switch
    {
        null or <= 0 => null,
        1 => "1 person has booked this event",
        var n => $"{n} people have booked this event",
    };
}

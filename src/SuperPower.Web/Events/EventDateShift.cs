using System.Globalization;
using System.Text.Json.Nodes;

namespace SuperPower.Web.Events;

/// <summary>
/// Moves the committed event dates to the import day (ADR-0037): whole days from the reference day to the
/// Europe/London import day, local time kept, offset recomputed.
/// </summary>
public sealed class EventDateShift(TimeProvider timeProvider)
{
    /// <summary>The day the committed event dates were written for.</summary>
    public static readonly DateOnly ReferenceDay = new(2026, 10, 16);

    private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    /// <summary>Pure calculation: shifts <paramref name="original"/> by the days from the reference day to <paramref name="importDay"/>.</summary>
    public static DateTimeOffset ShiftStart(DateTimeOffset original, DateOnly importDay)
    {
        var days = importDay.DayNumber - ReferenceDay.DayNumber;
        var local = TimeZoneInfo.ConvertTime(original, London).DateTime.AddDays(days);
        return new DateTimeOffset(local, London.GetUtcOffset(local));
    }

    /// <summary>Shifts the <c>date</c> of a startDate JSON value using the clock; <c>timeZone</c> and other fields are kept.</summary>
    public string ShiftStartJson(string json)
    {
        var node = JsonNode.Parse(json)!;
        var original = node["date"]!.GetValue<DateTimeOffset>();
        var importDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), London).DateTime);
        node["date"] = ShiftStart(original, importDay).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        return node.ToJsonString();
    }
}

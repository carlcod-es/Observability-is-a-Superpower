using System.Globalization;
using System.Text.Json.Nodes;
using Xunit;
using SuperPower.Web.Events;

namespace SuperPower.Web.Tests;

public class EventDateShiftTests
{
    private sealed class FakeTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private const string Committed = """{"date": "2026-10-20T12:50:00+01:00", "timeZone": "Europe/London"}""";

    private static EventDateShift At(int y, int m, int d) => new(new FakeTime(new DateTimeOffset(y, m, d, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void ImportedLater_ShiftsDaysAndRecomputesOffset()
    {
        var result = EventDateShift.ShiftStart(new DateTimeOffset(2026, 10, 20, 12, 50, 0, TimeSpan.FromHours(1)), new DateOnly(2026, 11, 26));
        Assert.Equal(new DateTimeOffset(2026, 11, 30, 12, 50, 0, TimeSpan.Zero), result);
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void ImportedOnReferenceDay_ChangesNothing()
    {
        var original = new DateTimeOffset(2026, 10, 20, 12, 50, 0, TimeSpan.FromHours(1));
        var result = EventDateShift.ShiftStart(original, EventDateShift.ReferenceDay);
        Assert.Equal(original, result);
        Assert.Equal(original.Offset, result.Offset);
    }

    [Fact]
    public void CrossingTimeChange_KeepsLocalTime()
    {
        // Clocks go back on 25 Oct 2026: 28 Oct 13:57 GMT shifted into summer time keeps 13:57 local.
        var result = EventDateShift.ShiftStart(new DateTimeOffset(2026, 10, 28, 13, 57, 0, TimeSpan.Zero), new DateOnly(2027, 4, 16));
        Assert.Equal(new DateTime(2027, 4, 28, 13, 57, 0), result.DateTime);
        Assert.Equal(TimeSpan.FromHours(1), result.Offset);
    }

    [Fact]
    public void Json_RoundTripsWithTimeZoneKept()
    {
        var json = At(2026, 11, 26).ShiftStartJson(Committed);
        var node = JsonNode.Parse(json)!;
        Assert.Equal("Europe/London", node["timeZone"]!.GetValue<string>());
        var date = node["date"]!.GetValue<DateTimeOffset>();
        Assert.Equal(new DateTimeOffset(2026, 11, 30, 12, 50, 0, TimeSpan.Zero), date);
        Assert.Equal(TimeSpan.Zero, date.Offset);
    }

    [Fact]
    public void Json_UnderThaiCulture_WritesGregorianIsoDate()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            var node = JsonNode.Parse(At(2026, 11, 26).ShiftStartJson(Committed))!;
            Assert.Equal("2026-11-30T12:50:00+00:00", node["date"]!.GetValue<string>());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Json_AtReferenceDay_KeepsDate()
    {
        var node = JsonNode.Parse(At(2026, 10, 16).ShiftStartJson(Committed))!;
        Assert.Equal(new DateTimeOffset(2026, 10, 20, 12, 50, 0, TimeSpan.FromHours(1)), node["date"]!.GetValue<DateTimeOffset>());
    }
}

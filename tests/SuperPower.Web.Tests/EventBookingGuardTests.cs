using Xunit;
using SuperPower.Web.Events;
using SuperPower.Web.Forms;

namespace SuperPower.Web.Tests;

public class EventBookingGuardTests
{
    private sealed class FakeTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly EventSchedule Schedule = new(new FakeTime(Now));

    [Fact]
    public void UpcomingEvent_CanBook() =>
        Assert.True(EventBookingGuard.CanBook("event", Now.AddDays(1), Schedule));

    [Fact]
    public void PastEvent_Rejected() =>
        Assert.False(EventBookingGuard.CanBook("event", Now.AddDays(-1), Schedule));

    [Fact]
    public void MissingStartDate_Rejected() =>
        Assert.False(EventBookingGuard.CanBook("event", null, Schedule));

    [Theory]
    [InlineData("service")]
    [InlineData(null)]
    public void NotAnEventPage_Rejected(string? alias) =>
        Assert.False(EventBookingGuard.CanBook(alias, Now.AddDays(1), Schedule));
}

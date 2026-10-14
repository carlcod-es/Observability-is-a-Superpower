using Xunit;
using SuperPower.Web.Events;

namespace SuperPower.Web.Tests;

public class EventScheduleTests
{
    private sealed class FakeTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    private static EventSchedule Schedule() => new(new FakeTime(Now));

    [Fact]
    public void StartEqualToNow_IsPast() => Assert.True(Schedule().IsPast(Now));

    [Fact]
    public void StartOneSecondAfterNow_IsUpcoming() => Assert.False(Schedule().IsPast(Now.AddSeconds(1)));

    [Fact]
    public void StartOneTickAfterNow_IsUpcoming() => Assert.False(Schedule().IsPast(Now.AddTicks(1)));

    [Fact]
    public void StartBeforeNow_IsPast() => Assert.True(Schedule().IsPast(Now.AddSeconds(-1)));

    [Fact]
    public void OffsetStartEqualToNow_IsPast() =>
        Assert.True(Schedule().IsPast(new DateTimeOffset(2026, 10, 22, 13, 0, 0, TimeSpan.FromHours(1))));

    [Fact]
    public void OffsetStartLooksEarlierButIsLater_IsUpcoming() =>
        // 12:30+01:00 = 11:30Z (past); 13:30+01:00 = 12:30Z (upcoming)
        Assert.False(Schedule().IsPast(new DateTimeOffset(2026, 10, 22, 13, 30, 0, TimeSpan.FromHours(1))));

    [Fact]
    public void OffsetStartLooksLaterButIsEarlier_IsPast() =>
        Assert.True(Schedule().IsPast(new DateTimeOffset(2026, 10, 22, 12, 30, 0, TimeSpan.FromHours(1))));
}

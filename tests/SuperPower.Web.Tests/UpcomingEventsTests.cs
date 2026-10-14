using Xunit;
using SuperPower.Web.Events;

namespace SuperPower.Web.Tests;

public class UpcomingEventsTests
{
    private sealed class FakeTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    private static UpcomingEvents Sut() => new(new EventSchedule(new FakeTime(Now)));

    private static IReadOnlyList<string> Run(params (string, DateTimeOffset?)[] c) => Sut().Filter(c);

    [Fact]
    public void Empty_ReturnsEmpty() => Assert.Empty(Run());

    [Fact]
    public void UnsortedInput_ComesOutAscending() =>
        Assert.Equal(["a", "b", "c"], Run(("c", Now.AddDays(3)), ("a", Now.AddDays(1)), ("b", Now.AddDays(2))));

    [Fact]
    public void Ties_KeepInputOrder() =>
        Assert.Equal(["x", "y", "z"], Run(("x", Now.AddDays(1)), ("y", Now.AddDays(1)), ("z", Now.AddDays(1))));

    [Fact]
    public void StartEqualToNow_Excluded() => Assert.Empty(Run(("e", Now)));

    [Fact]
    public void StartJustAfterNow_Included() => Assert.Equal(["e"], Run(("e", Now.AddTicks(1))));

    [Fact]
    public void PastExcluded_UpcomingKept() =>
        Assert.Equal(["future"], Run(("past", Now.AddSeconds(-1)), ("future", Now.AddSeconds(1))));

    [Fact]
    public void NullStart_Excluded() => Assert.Equal(["e"], Run(("none", null), ("e", Now.AddDays(1))));

    [Fact]
    public void OrdersByInstantAcrossOffsets() =>
        // 13:30+01:00 = 12:30Z is before 12:45Z although its local clock reads later
        Assert.Equal(["early", "late"], Run(
            ("late", new DateTimeOffset(2026, 10, 22, 12, 45, 0, TimeSpan.Zero)),
            ("early", new DateTimeOffset(2026, 10, 22, 13, 30, 0, TimeSpan.FromHours(1)))));
}

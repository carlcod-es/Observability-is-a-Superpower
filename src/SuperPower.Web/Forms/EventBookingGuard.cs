using SuperPower.Web.Events;

namespace SuperPower.Web.Forms;

/// <summary>Booking rule (ADR-0016): an event page with a readable start date that is not past. Fails closed.</summary>
public static class EventBookingGuard
{
    public const string EndedMessage = "This event has ended";

    public static bool CanBook(string? contentTypeAlias, DateTimeOffset? start, IEventSchedule schedule) =>
        contentTypeAlias == EventSchedule.EventAlias && start is { } s && !schedule.IsPast(s);
}

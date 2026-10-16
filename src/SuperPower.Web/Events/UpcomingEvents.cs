using Umbraco.Cms.Core.Models.PublishedContent;

namespace SuperPower.Web.Events;

/// <summary>Upcoming-events query (ADR-0014 rule: upcoming = not past).</summary>
public interface IUpcomingEvents
{
    /// <summary>
    /// Keeps candidates whose start is upcoming, ordered ascending by start. A null start is NOT upcoming
    /// (opposite default from <see cref="IEventSchedule.IsPast(IPublishedContent)"/>: a listing must not show events it cannot date).
    /// Ties keep input order (stable).
    /// </summary>
    IReadOnlyList<T> Filter<T>(IEnumerable<(T Item, DateTimeOffset? Start)> candidates);

    /// <summary>Upcoming <c>event</c> children of <paramref name="eventsParent"/>, ascending by start. Views apply any "take N".</summary>
    IReadOnlyList<IPublishedContent> GetUpcoming(IPublishedContent eventsParent);
}

public sealed class UpcomingEvents(IEventSchedule schedule) : IUpcomingEvents
{
    public IReadOnlyList<T> Filter<T>(IEnumerable<(T Item, DateTimeOffset? Start)> candidates) =>
        candidates
            .Where(c => c.Start is { } s && !schedule.IsPast(s))
            .OrderBy(c => c.Start!.Value.UtcDateTime) // OrderBy is stable
            .Select(c => c.Item)
            .ToList();

    public IReadOnlyList<IPublishedContent> GetUpcoming(IPublishedContent eventsParent) =>
        Filter(eventsParent.Children
            .Where(c => c.ContentType.Alias == EventSchedule.EventAlias)
            .Select(c => (c, c.GetProperty(EventSchedule.StartDateAlias)?.GetValue() as DateTimeOffset?)));
}

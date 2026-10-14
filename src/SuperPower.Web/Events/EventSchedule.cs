using Umbraco.Cms.Core.Models.PublishedContent;

namespace SuperPower.Web.Events;

/// <summary>Past-event rule (ADR-0014).</summary>
public interface IEventSchedule
{
    /// <summary>Past when <paramref name="start"/>, as an absolute instant, is at or before UTC now.</summary>
    bool IsPast(DateTimeOffset start);

    /// <summary>Reads <see cref="EventSchedule.StartDateAlias"/> from an event page. Missing value: not past.</summary>
    bool IsPast(IPublishedContent eventPage);
}

public sealed class EventSchedule(TimeProvider timeProvider) : IEventSchedule
{
    public const string EventAlias = "event";
    public const string StartDateAlias = "startDate";

    public bool IsPast(DateTimeOffset start) => start.UtcDateTime <= timeProvider.GetUtcNow().UtcDateTime;

    // Umbraco.DateTimeWithTimeZone converter yields DateTimeOffset? (Umbraco.Infrastructure 17.7.1).
    // Missing value: treated as not past, so the page still renders its form rather than hiding it silently.
    public bool IsPast(IPublishedContent eventPage) =>
        eventPage.GetProperty(StartDateAlias)?.GetValue() is DateTimeOffset start && IsPast(start);
}

using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using uSync.BackOffice;

namespace SuperPower.Web.Events;

/// <summary>Shifts event dates while uSync imports them (ADR-0037).</summary>
public sealed class EventImportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<EventDateShift>();
        builder.AddNotificationHandler<uSyncImportingItemNotification, EventImportHandler>();
    }
}

/// <summary>Rewrites <c>startDate</c> in the imported XML before uSync saves the item; the file on disk is not touched.</summary>
public sealed class EventImportHandler(EventDateShift shift) : INotificationHandler<uSyncImportingItemNotification>
{
    public void Handle(uSyncImportingItemNotification notification)
    {
        var item = notification.Item;
        if (item.Element("Info")?.Element("ContentType")?.Value != EventSchedule.EventAlias)
        {
            return;
        }

        var value = item.Element("Properties")?.Element(EventSchedule.StartDateAlias)?.Element("Value");
        if (value is null || string.IsNullOrWhiteSpace(value.Value))
        {
            return;
        }

        value.ReplaceNodes(new XCData(shift.ShiftStartJson(value.Value)));
    }
}

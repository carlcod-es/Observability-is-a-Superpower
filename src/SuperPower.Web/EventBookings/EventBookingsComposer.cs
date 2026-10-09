using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace SuperPower.Web.EventBookings;

public sealed class EventBookingsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, EventBookingMigrationRunner>();
        builder.Services.AddScoped<IEventBookingRepository, EventBookingRepository>();
    }
}

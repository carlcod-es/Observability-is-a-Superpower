using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using SuperPower.Web.Events;
using SuperPower.Web.Forms.Migrations;
using SuperPower.Web.Forms.Storage;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace SuperPower.Web.Forms;

/// <summary>Registers the site's forms services (ADR-0016).</summary>
public sealed class AcmeFormsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<SuperPower.Web.Events.IEventSchedule, SuperPower.Web.Events.EventSchedule>();
        builder.Services.AddSingleton<SuperPower.Web.Events.IUpcomingEvents, SuperPower.Web.Events.UpcomingEvents>();
        AddBookingServices(builder);
        builder.Services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, AcmeSiteMigrationRunner>();
    }

    private static void AddBookingServices(IUmbracoBuilder builder)
    {
        if (builder.Config.GetSection("services:api").GetChildren().Any())
        {
            var client = builder.Services.AddHttpClient<IBookingCounts, BookingCountsClient>(c => c.BaseAddress = new Uri("http://api"));
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental
            client.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
            client.AddStandardResilienceHandler(o =>
            {
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(2);
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
            });
        }
        else
        {
            builder.Services.AddSingleton<IBookingCounts, NoBookingCounts>();
        }

        if (!string.IsNullOrWhiteSpace(builder.Config.GetConnectionString("queue")))
        {
            builder.Services.AddSingleton<IBookingEvents, RabbitBookingEvents>();
        }
        else
        {
            builder.Services.AddSingleton<IBookingEvents, NoBookingEvents>();
        }
    }
}

using Microsoft.Extensions.DependencyInjection.Extensions;
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
        builder.Services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, AcmeSiteMigrationRunner>();
    }
}

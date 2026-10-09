using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace SuperPower.Web.EventBookings;

/// <summary>Runs <see cref="EventBookingMigrationPlan"/> at start-up once the runtime level is Run (ADR-0015).</summary>
public sealed class EventBookingMigrationRunner(
    IMigrationPlanExecutor migrationPlanExecutor,
    ICoreScopeProvider coreScopeProvider,
    IKeyValueService keyValueService,
    ILogger<EventBookingMigrationRunner> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        if (notification.RuntimeLevel != RuntimeLevel.Run)
        {
            return;
        }

        logger.LogInformation("Running migration plan {Plan}", EventBookingMigrationPlan.PlanName);
        var result = await new Upgrader(new EventBookingMigrationPlan())
            .ExecuteAsync(migrationPlanExecutor, coreScopeProvider, keyValueService);
        logger.LogInformation(
            "Migration plan {Plan} finished, final state {State}", EventBookingMigrationPlan.PlanName, result.FinalState);
    }
}

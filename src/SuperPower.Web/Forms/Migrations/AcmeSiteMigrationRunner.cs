using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace SuperPower.Web.Forms.Migrations;

/// <summary>Runs <see cref="AcmeSiteMigrationPlan"/> at start-up once the runtime level is Run (ADR-0016).</summary>
public sealed class AcmeSiteMigrationRunner(
    IMigrationPlanExecutor migrationPlanExecutor,
    ICoreScopeProvider coreScopeProvider,
    IKeyValueService keyValueService,
    ILogger<AcmeSiteMigrationRunner> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        if (notification.RuntimeLevel != RuntimeLevel.Run)
        {
            logger.LogInformation(
                "Migration plan {Plan} skipped, runtime level is {RuntimeLevel}",
                AcmeSiteMigrationPlan.PlanName, notification.RuntimeLevel);
            return;
        }

        logger.LogInformation("Running migration plan {Plan}", AcmeSiteMigrationPlan.PlanName);
        var result = await new Upgrader(new AcmeSiteMigrationPlan())
            .ExecuteAsync(migrationPlanExecutor, coreScopeProvider, keyValueService);
        logger.LogInformation(
            "Migration plan {Plan} finished, final state {State}", AcmeSiteMigrationPlan.PlanName, result.FinalState);
    }
}

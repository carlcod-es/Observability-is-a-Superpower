using Umbraco.Cms.Infrastructure.Migrations;
using SuperPower.Forms.Storage;

namespace SuperPower.Forms.Migrations;

/// <summary>Creates table <c>superPowerFormSubmission</c> if missing (ADR-0004). Provider-agnostic.</summary>
public class CreateSubmissionTableStep(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists(FormSubmissionDto.TableName))
        {
            Create.Table<FormSubmissionDto>().Do();
        }

        return Task.CompletedTask;
    }
}

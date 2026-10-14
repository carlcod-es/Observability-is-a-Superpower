using SuperPower.Web.Forms.Storage;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SuperPower.Web.Forms.Migrations;

/// <summary>Creates <c>acmeFormSubmission</c> if missing (ADR-0016). Provider-agnostic.</summary>
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

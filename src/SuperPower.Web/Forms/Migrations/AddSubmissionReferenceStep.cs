using NPoco;
using SuperPower.Web.Forms.Storage;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Extensions;

namespace SuperPower.Web.Forms.Migrations;

/// <summary>
/// Adds <c>ReferenceNumber</c> to <c>acmeFormSubmission</c>, backfills it from <c>Id</c>, adds the unique index
/// and the lock row (ADR-0026). Each part is skipped when already present (fresh installs get column and
/// index from the DTO in the first step).
/// </summary>
public class AddSubmissionReferenceStep(IMigrationContext context) : AsyncMigrationBase(context)
{
    private const string Column = "ReferenceNumber";
    private const string IndexName = "IX_acmeFormSubmission_ReferenceNumber";

    protected override async Task MigrateAsync()
    {
        var columns = SqlSyntax.GetColumnsInSchema(Context.Database).ToList();
        AddColumnIfNotExists<FormSubmissionDto>(columns, Column);

        var ids = await Database.FetchAsync<int>(
            Sql().Select<FormSubmissionDto>(x => x.Id)
                .From<FormSubmissionDto>()
                .WhereNull<FormSubmissionDto>(x => x.ReferenceNumber));
        foreach (var id in ids)
        {
            await Database.ExecuteAsync(
                Sql().Update<FormSubmissionDto>(u => u.Set(x => x.ReferenceNumber, id))
                    .Where<FormSubmissionDto>(x => x.Id == id));
        }

        if (!IndexExists(IndexName))
        {
            Create.Index(IndexName).OnTable(FormSubmissionDto.TableName)
                .OnColumn(Column).Ascending().WithOptions().Unique().Do();
        }

        var lockCount = await Database.ExecuteScalarAsync<int>(
            Sql().SelectCount().From<LockRow>().Where<LockRow>(x => x.Id == AcmeLocks.Submissions));
        if (lockCount == 0)
        {
            Insert.IntoTable("umbracoLock")
                .Row(new { id = AcmeLocks.Submissions, value = 1, name = AcmeLocks.Name }).Do();
        }
    }
}

/// <summary>Read-only mapping of Umbraco's internal <c>umbracoLock</c> table, for the existence check.</summary>
[TableName("umbracoLock")]
[ExplicitColumns]
internal sealed class LockRow
{
    [Column("id")]
    public int Id { get; set; }
}

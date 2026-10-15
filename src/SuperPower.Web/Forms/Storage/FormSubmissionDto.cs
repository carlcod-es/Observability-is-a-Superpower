using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SuperPower.Web.Forms.Storage;

/// <summary>Row of <c>acmeFormSubmission</c> (ADR-0016). Deliberately has no IP or client columns.</summary>
[TableName(TableName)]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public sealed class FormSubmissionDto
{
    public const string TableName = "acmeFormSubmission";

    // Dual annotations are intentional: NPoco [PrimaryKey] for inserts, Umbraco [PrimaryKeyColumn] for DDL.
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("FormKey")]
    [Index(IndexTypes.NonClustered, Name = "IX_acmeFormSubmission_FormKey")]
    public Guid FormKey { get; set; }

    [Column("PageKey")]
    public Guid PageKey { get; set; }

    [Column("SubmittedUtc")]
    public DateTime SubmittedUtc { get; set; }

    // Nullable in the database so the column can be added to tables with rows; the migration backfills it.
    [Column("ReferenceNumber")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Index(IndexTypes.UniqueNonClustered, Name = "IX_acmeFormSubmission_ReferenceNumber")]
    public int ReferenceNumber { get; set; }

    [Column("FieldsJson")]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string FieldsJson { get; set; } = "{}";
}

using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SuperPower.Forms.Storage;

/// <summary>Row of <c>superPowerFormSubmission</c> (ADR-0004). Deliberately has no IP or client columns.</summary>
[TableName(TableName)]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public sealed class FormSubmissionDto
{
    public const string TableName = "superPowerFormSubmission";

    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("FormKey")]
    [Index(IndexTypes.NonClustered, Name = "IX_superPowerFormSubmission_FormKey")]
    public Guid FormKey { get; set; }

    [Column("PageKey")]
    public Guid PageKey { get; set; }

    [Column("SubmittedUtc")]
    public DateTime SubmittedUtc { get; set; }

    [Column("FieldsJson")]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string FieldsJson { get; set; } = "{}";
}

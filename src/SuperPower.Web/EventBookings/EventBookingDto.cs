using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SuperPower.Web.EventBookings;

/// <summary>Row of <c>acmeEventBooking</c> (ADR-0015).</summary>
[TableName(TableName)]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public sealed class EventBookingDto
{
    public const string TableName = "acmeEventBooking";

    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("EventKey")]
    [Index(IndexTypes.NonClustered, Name = "IX_acmeEventBooking_EventKey")]
    public Guid EventKey { get; set; }

    /// <summary>Id of the stored form submission. No FK: package and site plans have no guaranteed order.</summary>
    [Column("SubmissionId")]
    [Index(IndexTypes.NonClustered, Name = "IX_acmeEventBooking_SubmissionId")]
    public int SubmissionId { get; set; }

    [Column("Places")]
    public int Places { get; set; }

    [Column("Status")]
    [Length(16)]
    public string Status { get; set; } = EventBookingStatus.Pending;

    [Column("CreatedUtc")]
    public DateTime CreatedUtc { get; set; }
}

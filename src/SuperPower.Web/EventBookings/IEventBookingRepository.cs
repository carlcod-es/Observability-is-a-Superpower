namespace SuperPower.Web.EventBookings;

/// <summary>Storage for <c>acmeEventBooking</c>. Requires an ambient Umbraco scope; it neither opens nor completes one.</summary>
public interface IEventBookingRepository
{
    Task<EventBooking> InsertAsync(
        Guid eventKey, int submissionId, int places, string status, DateTime createdUtc,
        CancellationToken cancellationToken);

    Task<EventBooking?> GetBySubmissionIdAsync(int submissionId, CancellationToken cancellationToken);
}

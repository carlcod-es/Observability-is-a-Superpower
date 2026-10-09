namespace SuperPower.Web.EventBookings;

public sealed record EventBooking(int Id, Guid EventKey, int SubmissionId, int Places, string Status, DateTime CreatedUtc);

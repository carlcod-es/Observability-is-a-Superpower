namespace SuperPower.Web.Forms;

/// <summary>Stable keys stored in <c>acmeFormSubmission.FormKey</c>, one per fixed form (ADR-0016).</summary>
public static class AcmeFormKeys
{
    public static readonly Guid Contact = new("a1c0f3d2-5b7e-4c81-9a2d-0e6f4b8c1a01");
    public static readonly Guid ServiceEnquiry = new("a1c0f3d2-5b7e-4c81-9a2d-0e6f4b8c1a02");
    public static readonly Guid EventBooking = new("a1c0f3d2-5b7e-4c81-9a2d-0e6f4b8c1a03");
}

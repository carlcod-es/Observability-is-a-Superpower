namespace SuperPower.Web.Forms;

/// <summary>Display form of a submission reference number (ADR-0026).</summary>
public static class SubmissionReference
{
    public static string Format(int number) => $"ACME-{number:D4}";
}

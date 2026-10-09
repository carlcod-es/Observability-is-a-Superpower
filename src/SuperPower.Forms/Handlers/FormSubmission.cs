namespace SuperPower.Forms;

/// <summary>A stored submission. <see cref="Values"/> is keyed by field alias.</summary>
public sealed record FormSubmission(
    int Id,
    Guid FormKey,
    Guid PageKey,
    DateTime SubmittedUtc,
    IReadOnlyDictionary<string, string> Values);

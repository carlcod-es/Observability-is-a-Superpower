namespace SuperPower.Web.Forms.Storage;

/// <summary>A stored submission; <paramref name="Fields"/> are the form's field values in property order.</summary>
public sealed record FormSubmission(
    int Id, Guid FormKey, Guid PageKey, DateTime SubmittedUtc, IReadOnlyList<KeyValuePair<string, string>> Fields);

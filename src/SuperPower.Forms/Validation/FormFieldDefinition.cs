namespace SuperPower.Forms;

/// <summary>A form field as the validator sees it. Mapped from the Block List by the form reader.</summary>
public sealed record FormFieldDefinition(string Alias, string Label, FormFieldType Type)
{
    public bool Required { get; init; }
    public string? Pattern { get; init; }
    public string? PatternMessage { get; init; }

    /// <summary>Allowed values; empty unless <see cref="Type"/> is Select.</summary>
    public IReadOnlyList<string> Options { get; init; } = [];
}

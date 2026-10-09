namespace SuperPower.Forms;

/// <param name="Errors">Alias to error message.</param>
/// <param name="Values">Alias to normalised value for every defined field, fixed values applied. This is what gets stored.</param>
public sealed record FormValidationResult(
    IReadOnlyDictionary<string, string> Errors,
    IReadOnlyDictionary<string, string> Values)
{
    public bool IsValid => Errors.Count == 0;
}

namespace SuperPower.Forms;

/// <summary>Pure helpers for the post action: only defined aliases are read; nothing else posted is kept.</summary>
public static class PostedValues
{
    /// <summary>
    /// One entry per defined field alias (null when not posted). Multiple posted values for one key
    /// (e.g. checkbox with hidden companion) are reduced to the last one.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> Collect(
        IEnumerable<FormFieldDefinition> fields, Func<string, string?> read)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var field in fields) result[field.Alias] = read(field.Alias);
        return result;
    }

    /// <summary>Aliases to echo back to the user on a failed post: every field that is not fixed by a handler.</summary>
    public static IReadOnlyList<string> EchoAliases(
        IEnumerable<FormFieldDefinition> fields, IReadOnlyDictionary<string, string> fixedValues) =>
        fields.Select(f => f.Alias).Where(a => !fixedValues.ContainsKey(a)).ToList();
}

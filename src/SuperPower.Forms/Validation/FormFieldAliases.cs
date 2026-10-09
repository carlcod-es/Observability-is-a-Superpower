namespace SuperPower.Forms;

public static class FormFieldAliases
{
    /// <summary>Aliases used more than once (ordinal, ignore case), one entry per duplicated alias.</summary>
    public static IReadOnlyList<string> FindDuplicates(IEnumerable<FormFieldDefinition> fields) =>
        fields
            .GroupBy(f => f.Alias, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
}

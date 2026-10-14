using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SuperPower.Web.Forms.Storage;

/// <summary>
/// Maps a form model's field values to the <c>FieldsJson</c> column (ADR-0016). Field order relies on
/// compiler declaration order (<c>MetadataToken</c>); null values are stored as empty strings.
/// </summary>
public static class SubmissionJson
{
    // Stored JSON is never embedded raw in HTML, so no HTML-safe escaping.
    private static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Public instance properties in declaration order; strings as-is, bools as "true"/"false".</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> ToFields(object model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.MetadataToken)
            .Select(p => new KeyValuePair<string, string>(p.Name, p.GetValue(model) switch
            {
                null => string.Empty,
                bool b => b ? "true" : "false",
                var v => v.ToString() ?? string.Empty,
            }))
            .ToList();
    }

    /// <summary>Compact JSON object; property order follows <paramref name="fields"/>.</summary>
    public static string Serialize(IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
        {
            writer.WriteStartObject();
            foreach (var (key, value) in fields)
            {
                writer.WriteString(key, value);
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}

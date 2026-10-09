using System.Text.Json;

namespace SuperPower.Forms.Storage;

/// <summary>Maps posted values (keyed by field alias) to the <c>FieldsJson</c> column and back.</summary>
public static class SubmissionJson
{
    /// <summary>Compact JSON object; property order follows the enumeration order of <paramref name="values"/>.</summary>
    public static string Serialize(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var (key, value) in values)
            {
                writer.WriteString(key, value);
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Reads a JSON object of string values, preserving property order.</summary>
    public static IReadOnlyDictionary<string, string> Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var result = new OrderedMap();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Submission fields JSON must be an object.");
        }

        foreach (var p in doc.RootElement.EnumerateObject())
        {
            result.Add(p.Name, p.Value.ValueKind == JsonValueKind.String
                ? p.Value.GetString()!
                : throw new JsonException($"Field '{p.Name}' is not a string."));
        }

        return result;
    }

    /// <summary>Insertion-ordered read-only dictionary.</summary>
    private sealed class OrderedMap : IReadOnlyDictionary<string, string>
    {
        private readonly List<KeyValuePair<string, string>> _items = [];
        private readonly Dictionary<string, string> _index = new(StringComparer.Ordinal);

        public void Add(string key, string value)
        {
            _index[key] = value;
            var i = _items.FindIndex(x => x.Key == key);
            if (i >= 0) { _items[i] = new(key, value); } else { _items.Add(new(key, value)); }
        }

        public string this[string key] => _index[key];
        public IEnumerable<string> Keys => _items.Select(x => x.Key);
        public IEnumerable<string> Values => _items.Select(x => x.Value);
        public int Count => _items.Count;
        public bool ContainsKey(string key) => _index.ContainsKey(key);
        public bool TryGetValue(string key, out string value) => _index.TryGetValue(key, out value!);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

using SuperPower.Forms.Storage;
using Xunit;

namespace SuperPower.Forms.Tests;

public class SubmissionJsonTests
{
    private static List<KeyValuePair<string, string>> Ordered(params (string, string)[] items) =>
        items.Select(i => new KeyValuePair<string, string>(i.Item1, i.Item2)).ToList();

    private sealed class ListMap(List<KeyValuePair<string, string>> items) : IReadOnlyDictionary<string, string>
    {
        public string this[string key] => items.First(x => x.Key == key).Value;
        public IEnumerable<string> Keys => items.Select(x => x.Key);
        public IEnumerable<string> Values => items.Select(x => x.Value);
        public int Count => items.Count;
        public bool ContainsKey(string key) => items.Any(x => x.Key == key);
        public bool TryGetValue(string key, out string value)
        {
            value = items.FirstOrDefault(x => x.Key == key).Value!;
            return ContainsKey(key);
        }
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void Round_trip_returns_same_values()
    {
        var map = new ListMap(Ordered(("name", "Ann"), ("email", "a@example.com")));
        var back = SubmissionJson.Deserialize(SubmissionJson.Serialize(map));
        Assert.Equal(map.ToList(), back.ToList());
    }

    [Fact]
    public void Serialize_is_compact_object_in_given_order()
    {
        var map = new ListMap(Ordered(("zeta", "1"), ("alpha", "2"), ("mid", "3")));
        Assert.Equal("{\"zeta\":\"1\",\"alpha\":\"2\",\"mid\":\"3\"}", SubmissionJson.Serialize(map));
    }

    [Fact]
    public void Deserialize_preserves_property_order()
    {
        var back = SubmissionJson.Deserialize("{\"b\":\"1\",\"a\":\"2\",\"c\":\"3\"}");
        Assert.Equal(["b", "a", "c"], back.Keys);
    }

    [Fact]
    public void Special_characters_survive()
    {
        var map = new ListMap(Ordered(("name", "Zoë \"Q\" \\ 日本"), ("message", "line1\nline2\r\n\ttab")));
        var back = SubmissionJson.Deserialize(SubmissionJson.Serialize(map));
        Assert.Equal("Zoë \"Q\" \\ 日本", back["name"]);
        Assert.Equal("line1\nline2\r\n\ttab", back["message"]);
    }

    [Fact]
    public void Empty_map_round_trips()
    {
        var json = SubmissionJson.Serialize(new Dictionary<string, string>());
        Assert.Equal("{}", json);
        Assert.Empty(SubmissionJson.Deserialize(json));
    }

    [Fact]
    public void Empty_string_value_is_kept()
    {
        var back = SubmissionJson.Deserialize(SubmissionJson.Serialize(new Dictionary<string, string> { ["x"] = "" }));
        Assert.Equal("", back["x"]);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"a\":1}")]
    public void Deserialize_rejects_non_string_objects(string json) =>
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => SubmissionJson.Deserialize(json));
}

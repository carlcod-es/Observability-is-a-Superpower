using Xunit;

namespace SuperPower.Forms.Tests;

public class PostedValuesTests
{
    private static readonly FormFieldDefinition[] Fields =
    [
        new("name", "Name", FormFieldType.Text),
        new("email", "Email", FormFieldType.Email),
    ];

    [Fact]
    public void Collect_reads_only_defined_aliases()
    {
        var read = new List<string>();
        var posted = PostedValues.Collect(Fields, a => { read.Add(a); return a == "name" ? "Ann" : null; });

        Assert.Equal(["name", "email"], read);
        Assert.Equal(2, posted.Count);
        Assert.Equal("Ann", posted["name"]);
        Assert.Null(posted["email"]);
    }

    [Fact]
    public void EchoAliases_excludes_fixed_fields()
    {
        var fixedValues = new Dictionary<string, string> { ["email"] = "x@y.z" };
        Assert.Equal(["name"], PostedValues.EchoAliases(Fields, fixedValues));
    }
}

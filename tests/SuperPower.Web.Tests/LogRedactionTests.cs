using Microsoft.Extensions.Hosting;
using Xunit;

namespace SuperPower.Web.Tests;

public class LogRedactionTests
{
    private static readonly byte[] Key = "test-key-0123456789-test-key-012"u8.ToArray();

    private static List<KeyValuePair<string, object?>> Attrs(params (string Key, object? Value)[] items) =>
        items.Select(i => new KeyValuePair<string, object?>(i.Key, i.Value)).ToList();

    private static string? Value(IReadOnlyList<KeyValuePair<string, object?>> attrs, string key) =>
        attrs.Single(a => a.Key == key).Value as string;

    private static string Id(string stand) => stand["[REDACTED:".Length..^1];

    [Fact]
    public void Flat_blocked_name_is_redacted()
    {
        var (attrs, _) = LogRedaction.Redact(Attrs(("Name", "Ann Lee"), ("Form", "contact")), null, Key);
        Assert.Equal("[REDACTED]", Value(attrs, "Name"));
        Assert.Equal("contact", Value(attrs, "Form"));
    }

    [Fact]
    public void Dotted_blocked_name_is_redacted()
    {
        var (attrs, _) = LogRedaction.Redact(Attrs(("Model.Company", "Initech"), ("Model.Form", "x")), null, Key);
        Assert.Equal("[REDACTED]", Value(attrs, "Model.Company"));
        Assert.Equal("x", Value(attrs, "Model.Form"));
    }

    [Theory]
    [InlineData("NAME")]
    [InlineData("model.message")]
    [InlineData("Model.WEBSITE")]
    public void Names_are_case_insensitive(string name)
    {
        var (attrs, _) = LogRedaction.Redact(Attrs((name, "secret")), null, Key);
        Assert.Equal("[REDACTED]", Value(attrs, name));
    }

    [Fact]
    public void Email_valued_field_gets_stand_in()
    {
        var (attrs, _) = LogRedaction.Redact(Attrs(("Model.Email", "ann@example.com")), null, Key);
        Assert.Matches(@"^\[REDACTED:[0-9a-f]{12}\]$", Value(attrs, "Model.Email"));
    }

    [Fact]
    public void Non_email_value_in_email_field_is_plain_redacted()
    {
        var (attrs, _) = LogRedaction.Redact(Attrs(("Email", "not an email")), null, Key);
        Assert.Equal("[REDACTED]", Value(attrs, "Email"));
    }

    [Fact]
    public void Collected_values_are_replaced_in_message()
    {
        var (_, message) = LogRedaction.Redact(
            Attrs(("Name", "Ann Lee"), ("Notes", "Ann")), "Got Ann Lee with note Ann", Key);
        Assert.Equal("Got [REDACTED] with note [REDACTED]", message);
    }

    [Fact]
    public void Quoted_value_is_replaced_in_escaped_message_form()
    {
        var (_, message) = LogRedaction.Redact(
            Attrs(("Message", "He said \"hi\"")), "{ Message: \"He said \\\"hi\\\"\" }", Key);
        Assert.DoesNotContain("hi", message);
        Assert.Equal("{ Message: \"[REDACTED]\" }", message);
    }

    [Fact]
    public void Email_field_value_in_message_uses_same_stand_in()
    {
        var (attrs, message) = LogRedaction.Redact(
            Attrs(("Email", "ann@example.com")), "Mail from ann@example.com", Key);
        Assert.Equal($"Mail from {Value(attrs, "Email")}", message);
    }

    [Fact]
    public void Email_in_free_text_is_masked_in_attributes_and_message()
    {
        var (attrs, message) = LogRedaction.Redact(
            Attrs(("Detail", "sent to bob@example.org today")), "sent to bob@example.org today", Key);
        Assert.Matches(@"^sent to \[REDACTED:[0-9a-f]{12}\] today$", Value(attrs, "Detail"));
        Assert.Matches(@"^sent to \[REDACTED:[0-9a-f]{12}\] today$", message);
        Assert.DoesNotContain("bob@", message);
    }

    [Fact]
    public void Same_email_any_case_gives_same_id()
    {
        var (a, _) = LogRedaction.Redact(Attrs(("Email", "Ann@Example.com")), null, Key);
        var (b, _) = LogRedaction.Redact(Attrs(("Email", "ann@example.COM")), null, Key);
        var (c, m) = LogRedaction.Redact(Attrs(("Detail", "x")), "ANN@example.com", Key);
        Assert.Equal(Value(a, "Email"), Value(b, "Email"));
        Assert.Equal(Id(Value(a, "Email")!), Id(m!));
    }

    [Fact]
    public void Different_email_gives_different_id()
    {
        var (a, _) = LogRedaction.Redact(Attrs(("Email", "ann@example.com")), null, Key);
        var (b, _) = LogRedaction.Redact(Attrs(("Email", "bob@example.com")), null, Key);
        Assert.NotEqual(Value(a, "Email"), Value(b, "Email"));
    }

    [Fact]
    public void Different_key_gives_different_id()
    {
        var other = "other-key-0123456789-other-key-0"u8.ToArray();
        var (a, _) = LogRedaction.Redact(Attrs(("Email", "ann@example.com")), null, Key);
        var (b, _) = LogRedaction.Redact(Attrs(("Email", "ann@example.com")), null, other);
        Assert.NotEqual(Value(a, "Email"), Value(b, "Email"));
    }

    [Fact]
    public void Original_format_is_left_untouched()
    {
        var (attrs, _) = LogRedaction.Redact(
            Attrs(("{OriginalFormat}", "Mail {Detail} a@b.com"), ("Detail", "c@d.com")), null, Key);
        Assert.Equal("Mail {Detail} a@b.com", Value(attrs, "{OriginalFormat}"));
        Assert.StartsWith("[REDACTED:", Value(attrs, "Detail"));
    }

    [Fact]
    public void Entry_without_matches_is_unchanged()
    {
        var input = Attrs(("Form", "contact"), ("Reference", "ACME-0001"), ("Count", 3));
        var (attrs, message) = LogRedaction.Redact(input, "Form contact stored as ACME-0001", Key);
        Assert.Same(input, attrs);
        Assert.Equal("Form contact stored as ACME-0001", message);
    }
}

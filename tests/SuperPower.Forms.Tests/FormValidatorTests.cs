using System.Diagnostics;
using Xunit;

namespace SuperPower.Forms.Tests;

public class FormValidatorTests
{
    private static readonly IReadOnlyDictionary<string, string> NoFixed = new Dictionary<string, string>();

    private static FormValidationResult Run(FormFieldDefinition field, string? posted, IReadOnlyDictionary<string, string>? fixedValues = null) =>
        RunMany([field], new Dictionary<string, string?> { [field.Alias] = posted }, fixedValues);

    private static FormValidationResult RunMany(FormFieldDefinition[] fields, Dictionary<string, string?> posted, IReadOnlyDictionary<string, string>? fixedValues = null) =>
        new FormValidator().Validate(fields, posted, fixedValues ?? NoFixed);

    private static FormFieldDefinition Text(bool required = false) => new("name", "Name", FormFieldType.Text) { Required = required };

    [Fact]
    public void Required_empty_fails()
    {
        var r = Run(Text(true), "");
        Assert.Equal("Name is required.", r.Errors["name"]);
    }

    [Fact]
    public void Required_missing_fails()
    {
        var r = RunMany([Text(true)], new Dictionary<string, string?>());
        Assert.Equal("Name is required.", r.Errors["name"]);
    }

    [Fact]
    public void Required_whitespace_fails()
    {
        var r = Run(Text(true), "   ");
        Assert.Equal("Name is required.", r.Errors["name"]);
    }

    [Fact]
    public void Required_checkbox_unchecked_fails()
    {
        var r = Run(new("ok", "Consent", FormFieldType.Checkbox) { Required = true }, null);
        Assert.Equal("Consent is required.", r.Errors["ok"]);
        Assert.Equal("false", r.Values["ok"]);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    public void Checkbox_checked_is_normalised_to_true(string posted)
    {
        var r = Run(new("ok", "Consent", FormFieldType.Checkbox) { Required = true }, posted);
        Assert.True(r.IsValid);
        Assert.Equal("true", r.Values["ok"]);
    }

    [Fact]
    public void Valid_email_passes_and_is_trimmed()
    {
        var r = Run(new("email", "Email", FormFieldType.Email), "  a@example.com ");
        Assert.True(r.IsValid);
        Assert.Equal("a@example.com", r.Values["email"]);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("Name <a@example.com>")]
    [InlineData("a@")]
    public void Invalid_email_fails(string value)
    {
        var r = Run(new("email", "Email", FormFieldType.Email), value);
        Assert.Equal("Enter a valid email address.", r.Errors["email"]);
    }

    private static FormFieldDefinition Pattern(string pattern, string? message = null) =>
        new("code", "Code", FormFieldType.Text) { Pattern = pattern, PatternMessage = message };

    [Fact]
    public void Pattern_match_passes()
        => Assert.True(Run(Pattern("[A-Z]{3}"), "ABC").IsValid);

    [Fact]
    public void Pattern_must_match_whole_value()
        => Assert.False(Run(Pattern("[A-Z]{3}"), "ABCD").IsValid);

    [Fact]
    public void Pattern_mismatch_uses_configured_message()
        => Assert.Equal("Three capitals.", Run(Pattern("[A-Z]{3}", "Three capitals."), "abc").Errors["code"]);

    [Fact]
    public void Pattern_mismatch_uses_default_message()
        => Assert.Equal("Code is not in the expected format.", Run(Pattern("[A-Z]{3}"), "abc").Errors["code"]);

    [Fact]
    public void Pattern_alternation_is_anchored_as_a_whole()
        => Assert.False(Run(Pattern("a|b"), "ab").IsValid);

    [Fact]
    public void Regex_timeout_counts_as_mismatch_and_completes_quickly()
    {
        var input = new string('a', 60) + "!";
        var sw = Stopwatch.StartNew();
        var r = Run(Pattern("(a+)+$", "Bad code."), input);
        sw.Stop();
        Assert.Equal("Bad code.", r.Errors["code"]);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2), $"took {sw.Elapsed}");
    }

    [Fact]
    public void Invalid_pattern_counts_as_mismatch_without_throwing()
        => Assert.Equal("Bad.", Run(Pattern("([", "Bad."), "x").Errors["code"]);

    private static FormFieldDefinition Places() =>
        new("places", "Places", FormFieldType.Select) { Options = ["1", "2", "3", "4"] };

    [Fact]
    public void Select_valid_option_passes()
        => Assert.True(Run(Places(), "3").IsValid);

    [Fact]
    public void Select_tampered_value_fails()
        => Assert.Equal("Choose one of the options.", Run(Places(), "5").Errors["places"]);

    [Fact]
    public void Select_membership_is_ordinal()
        => Assert.False(Run(new("c", "Colour", FormFieldType.Select) { Options = ["Red"] }, "red").IsValid);

    [Fact]
    public void Fixed_value_mismatch_fails()
    {
        var r = Run(Places(), "2", new Dictionary<string, string> { ["places"] = "1" });
        Assert.Equal("This value cannot be changed.", r.Errors["places"]);
    }

    [Fact]
    public void Fixed_value_is_applied_when_not_posted()
    {
        var r = RunMany([Places()], new Dictionary<string, string?>(), new Dictionary<string, string> { ["places"] = "1" });
        Assert.True(r.IsValid);
        Assert.Equal("1", r.Values["places"]);
    }

    [Fact]
    public void Fixed_value_matching_posted_passes()
    {
        var r = Run(Places(), "1", new Dictionary<string, string> { ["places"] = "1" });
        Assert.True(r.IsValid);
        Assert.Equal("1", r.Values["places"]);
    }

    [Fact]
    public void Optional_empty_skips_other_rules()
    {
        var field = new FormFieldDefinition("e", "Email", FormFieldType.Email) { Pattern = "x", PatternMessage = "no" };
        var r = Run(field, "");
        Assert.True(r.IsValid);
        Assert.Equal("", r.Values["e"]);
    }

    [Fact]
    public void Unknown_posted_keys_are_ignored()
    {
        var r = RunMany([Text()], new Dictionary<string, string?> { ["name"] = "A", ["evil"] = "x" });
        Assert.True(r.IsValid);
        Assert.Equal(["name"], r.Values.Keys);
    }

    [Fact]
    public void Values_trimmed_except_textarea()
    {
        var r = RunMany(
            [Text(), new("msg", "Message", FormFieldType.Textarea)],
            new Dictionary<string, string?> { ["name"] = " A ", ["msg"] = " hi\n" });
        Assert.Equal("A", r.Values["name"]);
        Assert.Equal(" hi\n", r.Values["msg"]);
    }

    [Fact]
    public void Textarea_whitespace_only_is_empty()
    {
        var r = Run(new("msg", "Message", FormFieldType.Textarea) { Required = true }, " \n ");
        Assert.Equal("Message is required.", r.Errors["msg"]);
        Assert.Equal("", r.Values["msg"]);
    }

    [Fact]
    public void Duplicate_aliases_are_found_case_insensitively()
    {
        var dups = FormFieldAliases.FindDuplicates([
            new("Email", "A", FormFieldType.Text), new("email", "B", FormFieldType.Text), new("other", "C", FormFieldType.Text)]);
        Assert.Single(dups);
        Assert.Equal("email", dups[0], ignoreCase: true);
    }

    [Fact]
    public void Unique_aliases_have_no_duplicates()
        => Assert.Empty(FormFieldAliases.FindDuplicates([new("a", "A", FormFieldType.Text), new("b", "B", FormFieldType.Text)]));
}

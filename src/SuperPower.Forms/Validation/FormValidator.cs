using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SuperPower.Forms;

/// <summary>Server-side validation (ADR-0005). Pure: no Umbraco types.</summary>
public sealed class FormValidator
{
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(100);

    public FormValidationResult Validate(
        IReadOnlyList<FormFieldDefinition> fields,
        IReadOnlyDictionary<string, string?> posted,
        IReadOnlyDictionary<string, string> fixedValues)
    {
        var errors = new Dictionary<string, string>();
        var values = new Dictionary<string, string>();

        foreach (var field in fields)
        {
            posted.TryGetValue(field.Alias, out var raw);
            var value = Normalise(field, raw);

            if (fixedValues.TryGetValue(field.Alias, out var fixedValue))
            {
                if (value.Length > 0 && value != fixedValue)
                {
                    errors[field.Alias] = "This value cannot be changed.";
                    values[field.Alias] = value;
                    continue;
                }
                value = fixedValue;
            }

            values[field.Alias] = value;
            var error = Check(field, value);
            if (error is not null) errors[field.Alias] = error;
        }

        return new FormValidationResult(errors, values);
    }

    private static string Normalise(FormFieldDefinition field, string? raw)
    {
        if (field.Type == FormFieldType.Checkbox)
            return string.Equals(raw?.Trim(), "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
        if (string.IsNullOrWhiteSpace(raw)) return "";
        return field.Type == FormFieldType.Textarea ? raw : raw.Trim();
    }

    private static string? Check(FormFieldDefinition field, string value)
    {
        if (field.Type == FormFieldType.Checkbox)
            return field.Required && value != "true" ? $"{field.Label} is required." : null;

        if (value.Length == 0)
            return field.Required ? $"{field.Label} is required." : null;

        if (field.Type == FormFieldType.Email && !IsValidEmail(value))
            return "Enter a valid email address.";

        if (field.Type == FormFieldType.Select && !field.Options.Contains(value, StringComparer.Ordinal))
            return "Choose one of the options.";

        if (!string.IsNullOrEmpty(field.Pattern) && !Matches(field.Pattern, value))
            return string.IsNullOrEmpty(field.PatternMessage)
                ? $"{field.Label} is not in the expected format."
                : field.PatternMessage;

        return null;
    }

    private static bool IsValidEmail(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value;

    private static bool Matches(string pattern, string value)
    {
        try
        {
            return Regex.IsMatch(value, $"^(?:{pattern})$", RegexOptions.CultureInvariant, PatternTimeout);
        }
        catch (RegexMatchTimeoutException) { return false; }
        catch (ArgumentException) { return false; }
    }
}

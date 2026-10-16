using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.Hosting;

/// <summary>Masks personal form data in log attributes and messages (ADR-0028).</summary>
internal static partial class LogRedaction
{
    private static readonly HashSet<string> BlockedFields =
        new(["Name", "Email", "Message", "Notes", "Company", "Website"], StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailPattern();

    public static (IReadOnlyList<KeyValuePair<string, object?>> Attributes, string? Message) Redact(
        IReadOnlyList<KeyValuePair<string, object?>> attributes, string? message, byte[] key)
    {
        var changed = false;
        var result = new KeyValuePair<string, object?>[attributes.Count];
        var collected = new Dictionary<string, string>();

        // 1. Blocked field names.
        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            result[i] = attribute;
            if (!BlockedFields.Contains(LastSegment(attribute.Key)))
            {
                continue;
            }

            string replacement;
            if (attribute.Value is string s && IsEmail(s))
            {
                replacement = Stand(s, key);
            }
            else
            {
                replacement = "[REDACTED]";
            }

            result[i] = new(attribute.Key, replacement);
            changed = true;
            if (attribute.Value is string { Length: > 0 } original)
            {
                collected[original] = replacement;
                if (original.Contains('"'))
                {
                    // Serilog renders destructured strings with " escaped as \".
                    collected[original.Replace("\"", "\\\"")] = replacement;
                }
            }
        }

        // 2. Collected values in the message, longest first, whole words only
        //    (a short value such as "te" must not hit "Submitted" or "Website").
        if (message is not null)
        {
            foreach (var (value, replacement) in collected.OrderByDescending(c => c.Key.Length))
            {
                var pattern = $@"(?<![\p{{L}}\p{{N}}_]){Regex.Escape(value)}(?![\p{{L}}\p{{N}}_])";
                var replaced = Regex.Replace(message, pattern, _ => replacement);
                if (replaced != message)
                {
                    message = replaced;
                    changed = true;
                }
            }
        }

        // 3. Emails in free text.
        for (var i = 0; i < result.Length; i++)
        {
            if (result[i].Key == "{OriginalFormat}" || result[i].Value is not string text
                || BlockedFields.Contains(LastSegment(result[i].Key)))
            {
                continue;
            }

            var masked = MaskEmails(text, key);
            if (!ReferenceEquals(masked, text) && masked != text)
            {
                result[i] = new(result[i].Key, masked);
                changed = true;
            }
        }

        if (message is not null)
        {
            var masked = MaskEmails(message, key);
            if (masked != message)
            {
                message = masked;
                changed = true;
            }
        }

        return changed ? (result, message) : (attributes, message);
    }

    private static string LastSegment(string name) => name[(name.LastIndexOf('.') + 1)..];

    private static bool IsEmail(string value) =>
        EmailPattern().Match(value) is { Success: true } m && m.Length == value.Length;

    private static string MaskEmails(string text, byte[] key) =>
        EmailPattern().Replace(text, m => Stand(m.Value, key));

    private static string Stand(string address, byte[] key)
    {
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(address.ToLowerInvariant()));
        return $"[REDACTED:{Convert.ToHexStringLower(hash)[..12]}]";
    }
}

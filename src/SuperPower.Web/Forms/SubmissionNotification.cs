using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace SuperPower.Web.Forms;

/// <summary>Owner notification text (ADR-0027). Pure; fixed wording.</summary>
public static class SubmissionNotification
{
    public static (string Subject, string Body) Build(
        string formName, string pageTitle, string reference, DateTime submittedUtc,
        IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        var body = new StringBuilder()
            .AppendLine($"A visitor submitted the {formName} form on the page \"{pageTitle}\".")
            .AppendLine()
            .AppendLine($"Reference: {reference}")
            .AppendLine($"Submitted (UTC): {submittedUtc:yyyy-MM-dd HH:mm:ss}")
            .AppendLine();
        foreach (var (label, value) in fields)
        {
            body.AppendLine($"{label}: {value}");
        }

        return ($"New {formName} submission {reference}", body.ToString());
    }

    /// <summary>Label/value pairs in declaration order; label from <c>[Display(Name)]</c>, else the property name.</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> LabelledFields(object model)
    {
        var props = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.MetadataToken).ToList();
        var values = Storage.SubmissionJson.ToFields(model);
        return props.Select((p, i) => new KeyValuePair<string, string>(
            p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name, values[i].Value)).ToList();
    }
}

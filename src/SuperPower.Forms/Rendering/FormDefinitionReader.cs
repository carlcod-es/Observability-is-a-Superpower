using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace SuperPower.Forms;

/// <summary>Reads a published form node into field definitions. Thin mapping; no logic worth a unit test.</summary>
public sealed class FormDefinitionReader
{
    public IReadOnlyList<FormFieldDefinition> ReadFields(IPublishedContent form)
    {
        var blocks = form.Value<BlockListModel>(SuperPowerFormsKeys.Properties.Fields);
        if (blocks is null) return [];

        var result = new List<FormFieldDefinition>();
        foreach (var block in blocks)
        {
            var element = block.Content;
            var type = MapType(element.ContentType.Alias);
            if (type is null) continue;

            result.Add(new FormFieldDefinition(
                element.Value<string>(SuperPowerFormsKeys.Properties.FieldAlias) ?? "",
                element.Value<string>(SuperPowerFormsKeys.Properties.Label) ?? "",
                type.Value)
            {
                Required = element.Value<bool>(SuperPowerFormsKeys.Properties.Required),
                Pattern = element.Value<string>(SuperPowerFormsKeys.Properties.Pattern),
                PatternMessage = element.Value<string>(SuperPowerFormsKeys.Properties.PatternMessage),
                Options = type == FormFieldType.Select
                    ? (element.Value<IEnumerable<string>>(SuperPowerFormsKeys.Properties.Options) ?? []).ToList()
                    : [],
            });
        }
        return result;
    }

    public string? ReadSuccessMessage(IPublishedContent form) =>
        form.Value<string>(SuperPowerFormsKeys.Properties.SuccessMessage);

    private static FormFieldType? MapType(string alias) => alias switch
    {
        SuperPowerFormsKeys.ElementTypes.TextAlias => FormFieldType.Text,
        SuperPowerFormsKeys.ElementTypes.EmailAlias => FormFieldType.Email,
        SuperPowerFormsKeys.ElementTypes.TextareaAlias => FormFieldType.Textarea,
        SuperPowerFormsKeys.ElementTypes.CheckboxAlias => FormFieldType.Checkbox,
        SuperPowerFormsKeys.ElementTypes.SelectAlias => FormFieldType.Select,
        _ => null,
    };
}

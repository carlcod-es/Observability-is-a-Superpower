using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;
using K = SuperPower.Forms.SuperPowerFormsKeys;

namespace SuperPower.Forms.Migrations;

/// <summary>
/// Creates options data type, five element types, Block List data type and the form document type, in that
/// order. Every object is looked up by fixed key first, so re-running creates no duplicates (ADR-0004).
/// </summary>
public class CreateSchemaStep(
    IPackagingService packagingService, IMediaService mediaService, MediaFileManager mediaFileManager,
    MediaUrlGeneratorCollection mediaUrlGenerators, IShortStringHelper shortStringHelper,
    IContentTypeBaseServiceProvider contentTypeBaseServiceProvider, IMigrationContext context,
    IOptions<PackageMigrationSettings> packageMigrationsSettings, IDataTypeService dataTypeService,
    IContentTypeService contentTypeService, PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer serializer)
    : AsyncPackageMigrationBase(packagingService, mediaService, mediaFileManager, mediaUrlGenerators,
        shortStringHelper, contentTypeBaseServiceProvider, context, packageMigrationsSettings)
{
    private static readonly (Guid Key, string Alias, string Name, string Icon)[] ElementTypeDefs =
    [
        (K.ElementTypes.Text, K.ElementTypes.TextAlias, "Text field", "icon-autofill"),
        (K.ElementTypes.Email, K.ElementTypes.EmailAlias, "Email field", "icon-message"),
        (K.ElementTypes.Textarea, K.ElementTypes.TextareaAlias, "Textarea field", "icon-notepad"),
        (K.ElementTypes.Checkbox, K.ElementTypes.CheckboxAlias, "Checkbox field", "icon-checkbox"),
        (K.ElementTypes.Select, K.ElementTypes.SelectAlias, "Select field", "icon-bulleted-list"),
    ];

    protected override async Task MigrateAsync()
    {
        var optionsType = await EnsureDataTypeAsync(
            K.DataTypes.Options, K.DataTypes.OptionsName, "Umbraco.MultipleTextstring", new Dictionary<string, object>());

        foreach (var def in ElementTypeDefs)
        {
            await EnsureElementTypeAsync(def, optionsType);
        }

        // Mirrors the backoffice-created Block List (uSync compPageMainContentBlockList.config):
        // {"blocks":[{"contentElementTypeKey":"..."}]}
        var blocks = ElementTypeDefs
            .Select(d => (object)new Dictionary<string, object> { ["contentElementTypeKey"] = d.Key })
            .ToList();
        var fieldsType = await EnsureDataTypeAsync(
            K.DataTypes.Fields, K.DataTypes.FieldsName, Constants.PropertyEditors.Aliases.BlockList,
            new Dictionary<string, object> { ["blocks"] = blocks });

        await EnsureFormDocumentTypeAsync(fieldsType);

        Context.Complete();
    }

    private async Task<IDataType> EnsureDataTypeAsync(
        Guid key, string name, string editorAlias, IDictionary<string, object> config)
    {
        var existing = await dataTypeService.GetAsync(key);
        if (existing is not null)
        {
            return existing;
        }

        var dataType = new DataType(propertyEditors[editorAlias], serializer, -1)
        {
            Key = key,
            Name = name,
            ConfigurationData = config,
        };
        var result = await dataTypeService.CreateAsync(dataType, Constants.Security.SuperUserKey);
        return result.Success
            ? result.Result!
            : throw new InvalidOperationException($"Creating data type '{name}' failed: {result.Status}");
    }

    private async Task EnsureElementTypeAsync(
        (Guid Key, string Alias, string Name, string Icon) def, IDataType optionsType)
    {
        if (await contentTypeService.GetAsync(def.Key) is not null)
        {
            return;
        }

        var text = await RequireDataTypeAsync(Constants.DataTypes.Guids.TextstringGuid);
        var toggle = await RequireDataTypeAsync(Constants.DataTypes.Guids.CheckboxGuid);

        var type = new ContentType(shortStringHelper, -1)
        {
            Key = def.Key,
            Alias = def.Alias,
            Name = def.Name,
            Icon = def.Icon,
            IsElement = true,
        };
        AddProp(type, text, K.Properties.FieldAlias, "Alias", mandatory: true);
        AddProp(type, text, K.Properties.Label, "Label", mandatory: true);
        AddProp(type, toggle, K.Properties.Required, "Required");
        AddProp(type, text, K.Properties.Pattern, "Pattern (regex)");
        AddProp(type, text, K.Properties.PatternMessage, "Pattern error message");
        if (def.Key == K.ElementTypes.Select)
        {
            AddProp(type, optionsType, K.Properties.Options, "Options");
        }

        await SaveAsync(type);
    }

    private async Task EnsureFormDocumentTypeAsync(IDataType fieldsType)
    {
        if (await contentTypeService.GetAsync(K.DocumentTypes.Form) is not null)
        {
            return;
        }

        var text = await RequireDataTypeAsync(Constants.DataTypes.Guids.TextstringGuid);
        var type = new ContentType(shortStringHelper, -1)
        {
            Key = K.DocumentTypes.Form,
            Alias = K.DocumentTypes.FormAlias,
            Name = "Form",
            Icon = "icon-poll",
            IsElement = false,
            AllowedAsRoot = false,
        };
        AddProp(type, fieldsType, K.Properties.Fields, "Fields");
        AddProp(type, text, K.Properties.SuccessMessage, "Success message");
        await SaveAsync(type);
    }

    private async Task<IDataType> RequireDataTypeAsync(Guid key) =>
        await dataTypeService.GetAsync(key) ?? throw new InvalidOperationException($"Built-in data type {key} missing");

    private void AddProp(ContentType type, IDataType dataType, string alias, string name, bool mandatory = false) =>
        type.AddPropertyType(
            new PropertyType(shortStringHelper, dataType) { Alias = alias, Name = name, Mandatory = mandatory },
            "content", "Content");

    private async Task SaveAsync(ContentType type)
    {
        var result = await contentTypeService.CreateAsync(type, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Creating content type '{type.Alias}' failed: {result.Result}");
        }
    }
}

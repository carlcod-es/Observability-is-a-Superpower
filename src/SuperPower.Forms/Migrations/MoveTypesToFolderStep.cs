using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using K = SuperPower.Forms.SuperPowerFormsKeys;

namespace SuperPower.Forms.Migrations;

/// <summary>
/// Puts the package's document types (five field element types and the form type) in a root-level
/// document type folder "SuperPower Forms". Idempotent; data types stay where they are.
/// </summary>
public class MoveTypesToFolderStep(
    IMigrationContext context,
    IContentTypeContainerService containerService,
    IContentTypeService contentTypeService,
    ILogger<MoveTypesToFolderStep> logger) : AsyncMigrationBase(context)
{
    private static readonly Guid[] TypeKeys =
    [
        K.ElementTypes.Text, K.ElementTypes.Email, K.ElementTypes.Textarea,
        K.ElementTypes.Checkbox, K.ElementTypes.Select, K.DocumentTypes.Form,
    ];

    protected override async Task MigrateAsync()
    {
        if (await containerService.GetAsync(K.Containers.DocumentTypes) is null)
        {
            var created = await containerService.CreateAsync(
                K.Containers.DocumentTypes, K.Containers.DocumentTypesName, null, Constants.Security.SuperUserKey);
            if (!created.Success)
            {
                throw new InvalidOperationException($"Could not create document type folder: {created.Status}");
            }

            logger.LogInformation("Created document type folder {Name}", K.Containers.DocumentTypesName);
        }

        foreach (var key in TypeKeys)
        {
            var type = await contentTypeService.GetAsync(key);
            if (type is null)
            {
                logger.LogWarning("Document type {Key} not found; not moved", key);
                continue;
            }

            var parent = await containerService.GetParentAsync(type);
            if (parent?.Key == K.Containers.DocumentTypes)
            {
                continue;
            }

            var moved = await contentTypeService.MoveAsync(key, K.Containers.DocumentTypes);
            if (!moved.Success)
            {
                throw new InvalidOperationException($"Could not move document type {type.Alias}: {moved.Status}");
            }

            logger.LogInformation("Moved document type {Alias} into folder {Name}", type.Alias, K.Containers.DocumentTypesName);
        }
    }
}

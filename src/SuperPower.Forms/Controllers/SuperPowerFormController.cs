using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using SuperPower.Forms.Storage;

namespace SuperPower.Forms;

/// <summary>
/// Handles form posts (ADR-0005, ADR-0013). Logs outcomes and field ALIASES only: never posted values.
/// </summary>
public sealed class SuperPowerFormController(
    IUmbracoContextAccessor umbracoContextAccessor,
    IUmbracoDatabaseFactory databaseFactory,
    ServiceContext services,
    AppCaches appCaches,
    IProfilingLogger profilingLogger,
    IPublishedUrlProvider publishedUrlProvider,
    FormDefinitionReader reader,
    FormValidator validator,
    FormSubmissionHandlers handlers,
    IFormSubmissionRepository repository,
    ICoreScopeProvider coreScopeProvider,
    TimeProvider timeProvider,
    ILogger<SuperPowerFormController> logger)
    : SurfaceController(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid formKey, CancellationToken cancellationToken)
    {
        var form = UmbracoContext.Content?.GetById(formKey);
        if (form is null || form.ContentType.Alias != SuperPowerFormsKeys.DocumentTypes.FormAlias)
        {
            logger.LogWarning("Form post rejected: no form with key {FormKey}", formKey);
            return NotFound();
        }

        var page = CurrentPage;
        if (page is null)
        {
            logger.LogWarning("Form post rejected: no current page for {FormName} ({FormKey})", form.Name, form.Key);
            return NotFound();
        }

        var fields = reader.ReadFields(form);
        var duplicates = FormFieldAliases.FindDuplicates(fields);
        if (duplicates.Count > 0)
        {
            logger.LogError(
                "Form post rejected: {FormName} ({FormKey}) has duplicate field aliases {DuplicateAliases}",
                form.Name, form.Key, duplicates);
            return BadRequest();
        }

        var preparation = handlers.Prepare(form, page);
        if (preparation.IsClosed)
        {
            logger.LogInformation(
                "Form post closed: {FormName} ({FormKey}) on page {PageKey}", form.Name, form.Key, page.Key);
            return CurrentUmbracoPage();
        }

        var posted = PostedValues.Collect(fields, alias => Request.Form[alias].LastOrDefault());
        var result = validator.Validate(fields, posted, preparation.FixedValues);

        if (!result.IsValid)
        {
            foreach (var alias in PostedValues.EchoAliases(fields, preparation.FixedValues))
            {
                var raw = posted[alias];
                ModelState.SetModelValue(alias, raw, raw);
            }
            foreach (var (alias, message) in result.Errors)
                ModelState.AddModelError(alias, message);

            logger.LogInformation(
                "Form post invalid: {FormName} ({FormKey}) on page {PageKey}, invalid fields {InvalidFields}",
                form.Name, form.Key, page.Key, result.Errors.Keys.ToList());
            return CurrentUmbracoPage();
        }

        // Insert and OnStored share one scope: an exception leaves it uncompleted, so nothing is stored.
        FormSubmission submission;
        using (var scope = coreScopeProvider.CreateCoreScope())
        {
            submission = await repository.InsertAsync(
                form.Key, page.Key, timeProvider.GetUtcNow().UtcDateTime, result.Values, cancellationToken);
            await handlers.OnStoredAsync(submission, page, cancellationToken);
            scope.Complete();
        }

        logger.LogInformation(
            "Form post stored: {FormName} ({FormKey}) on page {PageKey}, submission {SubmissionId}",
            form.Name, form.Key, page.Key, submission.Id);

        TempData[SuperPowerFormConstants.SubmittedTempDataKey] = form.Key.ToString();
        return RedirectToCurrentUmbracoPage();
    }
}

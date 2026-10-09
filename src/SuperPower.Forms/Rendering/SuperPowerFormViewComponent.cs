using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;

namespace SuperPower.Forms;

/// <summary>
/// Renders a SuperPower form (ADR-0005). Constraint: one SuperPowerForm per page, because post-back
/// state (ModelState keyed by field alias) is not scoped per form.
/// Post-back contract with the controller: after a failed post it returns CurrentUmbracoPage() with
/// ModelState entries keyed by field alias (attempted value and error message); after success it sets
/// TempData[<see cref="SuperPowerFormConstants.SubmittedTempDataKey"/>] to the form key and redirects.
/// </summary>
public sealed class SuperPowerFormViewComponent(
    FormDefinitionReader reader,
    FormSubmissionHandlers handlers,
    IUmbracoContextAccessor umbracoContextAccessor,
    ILogger<SuperPowerFormViewComponent> logger) : ViewComponent
{
    public IViewComponentResult Invoke(IPublishedContent form, IPublishedContent? page = null)
    {
        var fields = reader.ReadFields(form);

        var duplicates = FormFieldAliases.FindDuplicates(fields);
        if (duplicates.Count > 0)
        {
            foreach (var alias in duplicates)
                logger.LogError(
                    "Form {FormName} ({FormKey}) not rendered: duplicate field alias {Alias}",
                    form.Name, form.Key, alias);
            return Content(string.Empty);
        }

        page ??= umbracoContextAccessor.TryGetUmbracoContext(out var ctx) ? ctx.PublishedRequest?.PublishedContent : null;
        if (page is null)
        {
            logger.LogError("Form {FormName} ({FormKey}) not rendered: no current page", form.Name, form.Key);
            return Content(string.Empty);
        }

        var preparation = handlers.Prepare(form, page);

        var posted = new Dictionary<string, string>();
        var errors = new Dictionary<string, string>();
        foreach (var (key, entry) in ViewContext.ModelState)
        {
            if (key.Length == 0) continue;
            if (entry.AttemptedValue is not null) posted[key] = entry.AttemptedValue;
            var message = entry.Errors.FirstOrDefault()?.ErrorMessage;
            if (!string.IsNullOrEmpty(message)) errors[key] = message;
        }

        var submitted = TempData[SuperPowerFormConstants.SubmittedTempDataKey] is string s && s == form.Key.ToString();

        var model = SuperPowerFormViewModelBuilder.Build(
            form.Key, form.Name, fields, preparation, posted, errors, reader.ReadSuccessMessage(form), submitted);
        return View(model);
    }
}

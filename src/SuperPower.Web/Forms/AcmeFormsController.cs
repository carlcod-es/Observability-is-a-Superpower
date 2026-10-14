using Microsoft.AspNetCore.Mvc;
using SuperPower.Web.Events;
using SuperPower.Web.Forms.Storage;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace SuperPower.Web.Forms;

/// <summary>Posts for the three fixed forms (ADR-0016). Success: store one row, TempData flag, redirect (PRG).</summary>
public sealed class AcmeFormsController(
    IUmbracoContextAccessor umbracoContextAccessor,
    IUmbracoDatabaseFactory databaseFactory,
    ServiceContext services,
    AppCaches appCaches,
    IProfilingLogger profilingLogger,
    IPublishedUrlProvider publishedUrlProvider,
    ICoreScopeProvider scopeProvider,
    IFormSubmissionRepository repository,
    IEventSchedule schedule,
    TimeProvider timeProvider)
    : SurfaceController(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
{
    /// <summary>TempData key set on success; value is the form key (Guid string) so only that form shows the message.</summary>
    public const string SuccessTempDataKey = "AcmeFormSuccess";

    public const string SuccessMessage = "Thank you, we'll be in touch.";

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SubmitContact(ContactForm model) => Handle(AcmeFormKeys.Contact, model);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SubmitServiceEnquiry(ServiceEnquiryForm model) => Handle(AcmeFormKeys.ServiceEnquiry, model);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitEventBooking(EventBookingForm model)
    {
        var start = CurrentPage?.GetProperty(EventSchedule.StartDateAlias)?.GetValue() as DateTimeOffset?;
        if (!EventBookingGuard.CanBook(CurrentPage?.ContentType.Alias, start, schedule))
        {
            // Model-level error (empty key): the Event view/partial renders it as the ended message.
            ModelState.AddModelError(string.Empty, EventBookingGuard.EndedMessage);
            return CurrentUmbracoPage();
        }

        return await Handle(AcmeFormKeys.EventBooking, model);
    }

    private async Task<IActionResult> Handle(Guid formKey, object model)
    {
        if (!ModelState.IsValid || CurrentPage is null)
        {
            return CurrentUmbracoPage();
        }

        using (var scope = scopeProvider.CreateCoreScope())
        {
            await repository.InsertAsync(
                formKey,
                CurrentPage.Key,
                timeProvider.GetUtcNow().UtcDateTime,
                SubmissionJson.ToFields(model));
            scope.Complete();
        }

        TempData[SuccessTempDataKey] = formKey.ToString();
        return RedirectToCurrentUmbracoPage();
    }
}

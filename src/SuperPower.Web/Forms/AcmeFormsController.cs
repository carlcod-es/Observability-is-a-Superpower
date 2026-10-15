using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SuperPower.Web.Events;
using SuperPower.Web.Forms.Storage;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
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
    TimeProvider timeProvider,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<AcmeFormsController> logger)
    : SurfaceController(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
{
    /// <summary>TempData key set on success; value is the form key (Guid string) so only that form shows the message.</summary>
    public const string SuccessTempDataKey = "AcmeFormSuccess";

    /// <summary>TempData key set on success; value is the formatted reference (e.g. ACME-0042).</summary>
    public const string ReferenceTempDataKey = "AcmeFormReference";

    private const string EmailType = "AcmeFormSubmission";

    public const string SuccessMessage = "Thank you, we'll be in touch.";

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SubmitContact(ContactForm model) => Handle(AcmeFormKeys.Contact, "contact", "Contact", model);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SubmitServiceEnquiry(ServiceEnquiryForm model) =>
        Handle(AcmeFormKeys.ServiceEnquiry, "service_enquiry", "Service enquiry", model);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitEventBooking(EventBookingForm model)
    {
        var start = CurrentPage?.GetProperty(EventSchedule.StartDateAlias)?.GetValue() as DateTimeOffset?;
        if (!EventBookingGuard.CanBook(CurrentPage?.ContentType.Alias, start, schedule))
        {
            // Model-level error (empty key): the Event view/partial renders it as the ended message.
            ModelState.AddModelError(string.Empty, EventBookingGuard.EndedMessage);
            using var rejected = AcmeFormsTelemetry.ActivitySource.StartActivity("acme.forms.submit");
            Finish(rejected, "event_booking", Invalid);
            return CurrentUmbracoPage();
        }

        return await Handle(AcmeFormKeys.EventBooking, "event_booking", "Event booking", model);
    }

    private const string Stored = "stored";
    private const string Invalid = "invalid";
    private const string Failed = "failed";

    private static readonly System.Diagnostics.Metrics.Counter<long> Submissions =
        AcmeFormsTelemetry.Meter.CreateCounter<long>("acme.forms.submissions");

    /// <summary>Sets the outcome tag and counts the post once. Tags carry form slug and outcome only.</summary>
    private static void Finish(Activity? activity, string form, string outcome)
    {
        activity?.SetTag("acme.form", form);
        activity?.SetTag("acme.form.outcome", outcome);
        Submissions.Add(1, new KeyValuePair<string, object?>("acme.form", form),
            new KeyValuePair<string, object?>("acme.form.outcome", outcome));
    }

    private async Task<IActionResult> Handle(Guid formKey, string form, string formName, object model)
    {
        using var activity = AcmeFormsTelemetry.ActivitySource.StartActivity("acme.forms.submit");
        if (!ModelState.IsValid || CurrentPage is null)
        {
            Finish(activity, form, Invalid);
            return CurrentUmbracoPage();
        }

        // Demo code
        logger.LogInformation("Form Submitted {@Model}", model);

        try
        {
            var submittedUtc = timeProvider.GetUtcNow().UtcDateTime;
            int number;
            using (var scope = scopeProvider.CreateCoreScope())
            {
                number = await repository.InsertAsync(formKey, CurrentPage.Key, submittedUtc, SubmissionJson.ToFields(model));
                scope.Complete();
            }

            var reference = SubmissionReference.Format(number);
            activity?.SetTag("acme.submission.reference", reference);
            logger.LogInformation("Form {Form} stored as {Reference}", form, reference);

            await Notify(form, formName, reference, submittedUtc, model);

            Finish(activity, form, Stored);
            TempData[SuccessTempDataKey] = formKey.ToString();
            TempData[ReferenceTempDataKey] = reference;
            return RedirectToCurrentUmbracoPage();
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            Finish(activity, form, Failed);
            throw;
        }
    }

    private async Task Notify(string form, string formName, string reference, DateTime submittedUtc, object model)
    {
        var owner = configuration["Acme:Forms:OwnerEmail"];
        if (string.IsNullOrWhiteSpace(owner) || !emailSender.CanSendRequiredEmail())
        {
            return;
        }

        using var notify = AcmeFormsTelemetry.ActivitySource.StartActivity("acme.forms.notify");
        notify?.SetTag("acme.form", form);
        notify?.SetTag("acme.submission.reference", reference);
        try
        {
            var (subject, body) = SubmissionNotification.Build(
                formName, CurrentPage!.Name, reference, submittedUtc, SubmissionNotification.LabelledFields(model));
            await emailSender.SendAsync(new EmailMessage(null, owner, subject, body, false), EmailType, false, null);
        }
        catch (Exception ex)
        {
            notify?.AddException(ex);
            notify?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}

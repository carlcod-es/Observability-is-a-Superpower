using Umbraco.Cms.Core.Models.PublishedContent;

namespace SuperPower.Forms;

/// <summary>
/// Site hook (ADR-0013). Register zero or more through DI. Each handler decides whether it applies
/// (for example by the page's document type); one that does not apply returns
/// <see cref="FormPreparation.Open"/> and does nothing in <see cref="OnStoredAsync"/>.
/// </summary>
public interface IFormSubmissionHandler
{
    /// <summary>Runs on render and on post, before validation.</summary>
    FormPreparation Prepare(IPublishedContent form, IPublishedContent page);

    /// <summary>
    /// Runs after the submission insert. The controller wraps insert and this call in one Umbraco scope,
    /// so an exception rolls back the submission too.
    /// </summary>
    Task OnStoredAsync(FormSubmission submission, IPublishedContent page, CancellationToken cancellationToken);
}

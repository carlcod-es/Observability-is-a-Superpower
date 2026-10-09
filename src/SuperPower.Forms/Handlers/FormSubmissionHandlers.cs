using Umbraco.Cms.Core.Models.PublishedContent;

namespace SuperPower.Forms;

/// <summary>Runs all registered <see cref="IFormSubmissionHandler"/>s in registration order and combines results.</summary>
public sealed class FormSubmissionHandlers(IEnumerable<IFormSubmissionHandler> handlers)
{
    private readonly IFormSubmissionHandler[] _handlers = handlers.ToArray();

    /// <summary>
    /// Any closed result wins (the first closed message, in registration order). Otherwise fixed values merge;
    /// a later handler overrides an earlier one for the same alias.
    /// </summary>
    public FormPreparation Prepare(IPublishedContent form, IPublishedContent page)
    {
        var fixedValues = new Dictionary<string, string>();
        foreach (var handler in _handlers)
        {
            var result = handler.Prepare(form, page);
            if (result.ClosedMessage is not null) return FormPreparation.Closed(result.ClosedMessage);
            foreach (var (alias, value) in result.FixedValues) fixedValues[alias] = value;
        }
        return fixedValues.Count == 0 ? FormPreparation.Open : FormPreparation.WithFixedValues(fixedValues);
    }

    public async Task OnStoredAsync(FormSubmission submission, IPublishedContent page, CancellationToken cancellationToken)
    {
        foreach (var handler in _handlers)
            await handler.OnStoredAsync(submission, page, cancellationToken);
    }
}

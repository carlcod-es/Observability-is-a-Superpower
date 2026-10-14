using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SuperPower.Web.Forms.Storage;

public sealed class FormSubmissionRepository(IScopeAccessor scopeAccessor) : IFormSubmissionRepository
{
    private IUmbracoDatabase Database =>
        scopeAccessor.AmbientScope?.Database
        ?? throw new InvalidOperationException("FormSubmissionRepository requires an ambient Umbraco scope.");

    public async Task InsertAsync(
        Guid formKey, Guid pageKey, DateTime submittedUtc, IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        var dto = new FormSubmissionDto
        {
            FormKey = formKey,
            PageKey = pageKey,
            SubmittedUtc = submittedUtc,
            FieldsJson = SubmissionJson.Serialize(fields),
        };
        await Database.InsertAsync(dto);
    }
}

using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SuperPower.Web.Forms.Storage;

public sealed class FormSubmissionRepository(IScopeAccessor scopeAccessor) : IFormSubmissionRepository
{
    private IScope Scope =>
        scopeAccessor.AmbientScope
        ?? throw new InvalidOperationException("FormSubmissionRepository requires an ambient Umbraco scope.");

    public async Task<int> InsertAsync(
        Guid formKey, Guid pageKey, DateTime submittedUtc, IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        // Demo Code : Act 2
        // The write lock is taken here on the ambient scope; it is held until the caller completes/disposes the scope.
        // Scope.WriteLock(AcmeLocks.Submissions); // TEMP: disabled to reproduce duplicate references - revert
        var database = Scope.Database;
        var max = await database.ExecuteScalarAsync<int?>(
            database.SqlContext.Sql().SelectMax<FormSubmissionDto>(x => x.ReferenceNumber).From<FormSubmissionDto>());

        var dto = new FormSubmissionDto
        {
            ReferenceNumber = (max ?? 0) + 1,
            FormKey = formKey,
            PageKey = pageKey,
            SubmittedUtc = submittedUtc,
            FieldsJson = SubmissionJson.Serialize(fields),
        };
        await database.InsertAsync(dto);
        return dto.ReferenceNumber;
    }
}

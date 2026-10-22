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
        // Write lock removed: concurrent submissions can read the same MAX and insert a duplicate reference.
        // Fix: take the lock before reading MAX; it is held until the caller completes the scope.
        // Initail Demo State - Comment out
        Scope.WriteLock(AcmeLocks.Submissions);

        var database = Scope.Database;
        var max = await database.ExecuteScalarAsync<int?>(
            database.SqlContext.Sql().SelectMax<FormSubmissionDto>(x => x.ReferenceNumber).From<FormSubmissionDto>());

        // Demo Code : Act 2
        // Fallback if traffic is too low to hit the race: uncomment to fail submissions made on an even second.
        // Initail Demo State - Comment out, but uncomment if the audience doesn't trigger the error
        // if (DateTime.UtcNow.Second % 2 == 0) throw new InvalidOperationException($"Duplicate submission reference {(max ?? 0) + 1}.");

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

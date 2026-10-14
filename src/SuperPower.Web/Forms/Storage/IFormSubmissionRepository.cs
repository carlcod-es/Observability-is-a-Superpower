namespace SuperPower.Web.Forms.Storage;

/// <summary>
/// Stores and reads submissions in <c>acmeFormSubmission</c>. Requires an ambient Umbraco scope: the caller
/// creates it with <c>ICoreScopeProvider</c> and completes it.
/// </summary>
public interface IFormSubmissionRepository
{
    Task<FormSubmission> InsertAsync(
        Guid formKey, Guid pageKey, DateTime submittedUtc, IReadOnlyList<KeyValuePair<string, string>> fields);

    Task<FormSubmission?> GetAsync(int id);

    Task<string?> GetFieldsJsonAsync(int id);
}

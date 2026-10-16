namespace SuperPower.Web.Forms.Storage;

/// <summary>
/// Stores submissions in <c>acmeFormSubmission</c>. Requires an ambient Umbraco scope: the caller
/// creates it with <c>ICoreScopeProvider</c> and completes it.
/// </summary>
public interface IFormSubmissionRepository
{
    /// <summary>Allocates the next reference number under a write lock, inserts, returns the number.</summary>
    Task<int> InsertAsync(
        Guid formKey, Guid pageKey, DateTime submittedUtc, IReadOnlyList<KeyValuePair<string, string>> fields);
}

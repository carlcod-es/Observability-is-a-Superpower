namespace SuperPower.Forms.Storage;

/// <summary>
/// Stores and reads submissions in <c>superPowerFormSubmission</c>. Requires an ambient Umbraco scope: the caller
/// creates it with <c>ICoreScopeProvider</c> and completes it, so handlers (ADR-0013 OnStored) share the transaction.
/// </summary>
public interface IFormSubmissionRepository
{
    Task<FormSubmission> InsertAsync(
        Guid formKey, Guid pageKey, DateTime submittedUtc, IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken);

    Task<FormSubmission?> GetAsync(int id, CancellationToken cancellationToken);
}

using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SuperPower.Forms.Storage;

public sealed class FormSubmissionRepository(IScopeAccessor scopeAccessor) : IFormSubmissionRepository
{
    private IUmbracoDatabase Database =>
        scopeAccessor.AmbientScope?.Database
        ?? throw new InvalidOperationException("FormSubmissionRepository requires an ambient Umbraco scope.");

    public async Task<FormSubmission> InsertAsync(
        Guid formKey, Guid pageKey, DateTime submittedUtc, IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var utc = submittedUtc.Kind == DateTimeKind.Utc ? submittedUtc : submittedUtc.ToUniversalTime();
        var dto = new FormSubmissionDto
        {
            FormKey = formKey,
            PageKey = pageKey,
            SubmittedUtc = utc,
            FieldsJson = SubmissionJson.Serialize(values),
        };
        await Database.InsertAsync(dto);
        return new FormSubmission(dto.Id, formKey, pageKey, utc, values);
    }

    public async Task<FormSubmission?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var database = Database;
        Sql<ISqlContext> sql = database.SqlContext.Sql()
            .Select<FormSubmissionDto>()
            .From<FormSubmissionDto>()
            .Where<FormSubmissionDto>(x => x.Id == id);
        var dto = await database.FirstOrDefaultAsync<FormSubmissionDto>(sql);
        return dto is null
            ? null
            : new FormSubmission(
                dto.Id, dto.FormKey, dto.PageKey,
                DateTime.SpecifyKind(dto.SubmittedUtc, DateTimeKind.Utc),
                SubmissionJson.Deserialize(dto.FieldsJson));
    }
}

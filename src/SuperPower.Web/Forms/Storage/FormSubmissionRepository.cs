using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SuperPower.Web.Forms.Storage;

public sealed class FormSubmissionRepository(IScopeAccessor scopeAccessor) : IFormSubmissionRepository
{
    private IUmbracoDatabase Database =>
        scopeAccessor.AmbientScope?.Database
        ?? throw new InvalidOperationException("FormSubmissionRepository requires an ambient Umbraco scope.");

    public async Task<FormSubmission> InsertAsync(
        Guid formKey, Guid pageKey, DateTime submittedUtc, IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        var utc = submittedUtc.Kind == DateTimeKind.Utc ? submittedUtc : submittedUtc.ToUniversalTime();
        var dto = new FormSubmissionDto
        {
            FormKey = formKey,
            PageKey = pageKey,
            SubmittedUtc = utc,
            FieldsJson = SubmissionJson.Serialize(fields),
        };
        await Database.InsertAsync(dto);
        return new FormSubmission(dto.Id, formKey, pageKey, utc, fields);
    }

    public async Task<FormSubmission?> GetAsync(int id)
    {
        var dto = await GetDtoAsync(id);
        return dto is null
            ? null
            : new FormSubmission(
                dto.Id, dto.FormKey, dto.PageKey,
                DateTime.SpecifyKind(dto.SubmittedUtc, DateTimeKind.Utc),
                SubmissionJson.Deserialize(dto.FieldsJson));
    }

    public async Task<string?> GetFieldsJsonAsync(int id) => (await GetDtoAsync(id))?.FieldsJson;

    private async Task<FormSubmissionDto?> GetDtoAsync(int id)
    {
        var database = Database;
        Sql<ISqlContext> sql = database.SqlContext.Sql()
            .Select<FormSubmissionDto>()
            .From<FormSubmissionDto>()
            .Where<FormSubmissionDto>(x => x.Id == id);
        return await database.FirstOrDefaultAsync<FormSubmissionDto>(sql);
    }
}

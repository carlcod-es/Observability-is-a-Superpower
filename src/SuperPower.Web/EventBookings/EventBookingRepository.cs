using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SuperPower.Web.EventBookings;

public sealed class EventBookingRepository(IScopeAccessor scopeAccessor) : IEventBookingRepository
{
    private IUmbracoDatabase Database =>
        scopeAccessor.AmbientScope?.Database
        ?? throw new InvalidOperationException("EventBookingRepository requires an ambient Umbraco scope.");

    public async Task<EventBooking> InsertAsync(
        Guid eventKey, int submissionId, int places, string status, DateTime createdUtc,
        CancellationToken cancellationToken)
    {
        var utc = createdUtc.Kind == DateTimeKind.Utc ? createdUtc : createdUtc.ToUniversalTime();
        var dto = new EventBookingDto
        {
            EventKey = eventKey,
            SubmissionId = submissionId,
            Places = places,
            Status = status,
            CreatedUtc = utc,
        };
        await Database.InsertAsync(dto);
        return ToModel(dto);
    }

    public async Task<EventBooking?> GetBySubmissionIdAsync(int submissionId, CancellationToken cancellationToken)
    {
        var database = Database;
        Sql<ISqlContext> sql = database.SqlContext.Sql()
            .Select<EventBookingDto>()
            .From<EventBookingDto>()
            .Where<EventBookingDto>(x => x.SubmissionId == submissionId);
        var dto = await database.FirstOrDefaultAsync<EventBookingDto>(sql);
        return dto is null ? null : ToModel(dto);
    }

    private static EventBooking ToModel(EventBookingDto dto) =>
        new(dto.Id, dto.EventKey, dto.SubmissionId, dto.Places, dto.Status,
            DateTime.SpecifyKind(dto.CreatedUtc, DateTimeKind.Utc));
}

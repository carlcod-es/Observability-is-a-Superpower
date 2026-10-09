using Umbraco.Cms.Infrastructure.Migrations;

namespace SuperPower.Web.EventBookings;

/// <summary>Creates <c>acmeEventBooking</c> if missing (ADR-0015). Provider-agnostic.</summary>
public class CreateEventBookingTableStep(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists(EventBookingDto.TableName))
        {
            Create.Table<EventBookingDto>().Do();
        }

        return Task.CompletedTask;
    }
}

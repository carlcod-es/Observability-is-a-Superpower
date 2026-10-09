using Umbraco.Cms.Infrastructure.Migrations;

namespace SuperPower.Web.EventBookings;

public class EventBookingMigrationPlan : MigrationPlan
{
    public const string PlanName = "Acme.EventBookings";

    public static readonly Guid StepCreateTable = new("7c1f2b64-3e0a-4d59-9a1b-5d2e8f6a4c10");

    public EventBookingMigrationPlan() : base(PlanName) =>
        From(string.Empty).To<CreateEventBookingTableStep>(StepCreateTable);
}

using Umbraco.Cms.Infrastructure.Migrations;

namespace SuperPower.Web.Forms.Migrations;

public class AcmeSiteMigrationPlan : MigrationPlan
{
    public const string PlanName = "Acme.Site";

    /// <summary>Step <c>acmeFormSubmission</c>.</summary>
    public static readonly Guid StepAcmeFormSubmission = new("3b8e5a52-91c4-4f0d-a6e7-2d4c7b19f035");

    public AcmeSiteMigrationPlan() : base(PlanName) =>
        From(string.Empty).To<CreateSubmissionTableStep>(StepAcmeFormSubmission);
}

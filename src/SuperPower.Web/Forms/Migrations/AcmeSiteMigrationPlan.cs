using Umbraco.Cms.Infrastructure.Migrations;

namespace SuperPower.Web.Forms.Migrations;

public class AcmeSiteMigrationPlan : MigrationPlan
{
    public const string PlanName = "Acme.Site";

    /// <summary>Step <c>acmeFormSubmission</c>.</summary>
    public static readonly Guid StepAcmeFormSubmission = new("3b8e5a52-91c4-4f0d-a6e7-2d4c7b19f035");

    /// <summary>Step <c>ReferenceNumber</c> (ADR-0026).</summary>
    public static readonly Guid StepSubmissionReference = new("7d2f9c14-6e03-4b5a-8f1d-c3a5e90b2d68");

    public AcmeSiteMigrationPlan() : base(PlanName) =>
        From(string.Empty)
            .To<CreateSubmissionTableStep>(StepAcmeFormSubmission)
            .To<AddSubmissionReferenceStep>(StepSubmissionReference);
}

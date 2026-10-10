using Umbraco.Cms.Core.Packaging;

namespace SuperPower.Forms.Migrations;

public class SuperPowerFormsMigrationPlan : PackageMigrationPlan
{
    public SuperPowerFormsMigrationPlan() : base(SuperPowerFormsKeys.PlanName) { }

    protected override void DefinePlan() =>
        To<CreateSchemaStep>(SuperPowerFormsKeys.StepCreateSchema)
            .To<CreateSubmissionTableStep>(SuperPowerFormsKeys.StepCreateSubmissionTable)
            .To<MoveTypesToFolderStep>(SuperPowerFormsKeys.StepMoveTypesToFolder);
}

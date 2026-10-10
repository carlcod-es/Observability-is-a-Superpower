namespace SuperPower.Forms;

/// <summary>Fixed keys and aliases of the package's schema (ADR-0004). Never change after first install.</summary>
public static class SuperPowerFormsKeys
{
    public const string PlanName = "SuperPower.Forms";

    public static class ElementTypes
    {
        public static readonly Guid Text = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a1001");
        public static readonly Guid Email = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a1002");
        public static readonly Guid Textarea = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a1003");
        public static readonly Guid Checkbox = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a1004");
        public static readonly Guid Select = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a1005");

        public const string TextAlias = "superPowerTextField";
        public const string EmailAlias = "superPowerEmailField";
        public const string TextareaAlias = "superPowerTextareaField";
        public const string CheckboxAlias = "superPowerCheckboxField";
        public const string SelectAlias = "superPowerSelectField";
    }

    public static class DataTypes
    {
        public static readonly Guid Options = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a2001");
        public static readonly Guid Fields = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a2002");

        public const string OptionsName = "SuperPower Forms - Options";
        public const string FieldsName = "SuperPower Forms - Fields";
    }

    public static class Containers
    {
        public static readonly Guid DocumentTypes = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a5001");

        public const string DocumentTypesName = "SuperPower Forms";
    }

    public static class DocumentTypes
    {
        public static readonly Guid Form = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a3001");

        public const string FormAlias = "superPowerForm";
    }

    public static class Properties
    {
        public const string FieldAlias = "fieldAlias";
        public const string Label = "label";
        public const string Required = "required";
        public const string Pattern = "pattern";
        public const string PatternMessage = "patternMessage";
        public const string Options = "options";
        public const string Fields = "fields";
        public const string SuccessMessage = "successMessage";
    }

    public static readonly Guid StepCreateSchema = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a4001");
    public static readonly Guid StepCreateSubmissionTable = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a4002");
    public static readonly Guid StepMoveTypesToFolder = new("5c1f4b7a-0d6e-4c1b-9a53-1e2b7f0a4003");
}

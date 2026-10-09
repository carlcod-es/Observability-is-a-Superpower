namespace SuperPower.Forms;

/// <summary>Post target and post-back state names shared by the ViewComponent, the view and the controller.</summary>
public static class SuperPowerFormConstants
{
    public const string ControllerName = "SuperPowerForm";
    public const string ActionName = "Submit";

    /// <summary>TempData key holding the key (string) of the form that was just submitted successfully.</summary>
    public const string SubmittedTempDataKey = "SuperPowerForms.Submitted";
}

public sealed record SuperPowerFormFieldViewModel(
    string Alias,
    string Label,
    FormFieldType Type,
    bool Required,
    IReadOnlyList<string> Options,
    string Value,
    string? Error);

public sealed class SuperPowerFormViewModel
{
    public Guid FormKey { get; init; }
    public string FormName { get; init; } = "";

    /// <summary>Non-fixed fields only; empty when the form is closed.</summary>
    public IReadOnlyList<SuperPowerFormFieldViewModel> Fields { get; init; } = [];

    /// <summary>Non-null: the form is closed; render only this.</summary>
    public string? ClosedMessage { get; init; }

    /// <summary>Non-null only right after a successful submit of this form.</summary>
    public string? SuccessMessage { get; init; }

    public string ControllerName => SuperPowerFormConstants.ControllerName;
    public string ActionName => SuperPowerFormConstants.ActionName;
}

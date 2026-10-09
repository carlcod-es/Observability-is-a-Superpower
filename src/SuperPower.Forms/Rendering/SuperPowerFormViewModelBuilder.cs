namespace SuperPower.Forms;

public static class SuperPowerFormViewModelBuilder
{
    public static SuperPowerFormViewModel Build(
        Guid formKey,
        string formName,
        IReadOnlyList<FormFieldDefinition> fields,
        FormPreparation preparation,
        IReadOnlyDictionary<string, string>? postedValues,
        IReadOnlyDictionary<string, string>? errors,
        string? successMessage,
        bool submitted)
    {
        if (preparation.IsClosed)
            return new SuperPowerFormViewModel { FormKey = formKey, FormName = formName, ClosedMessage = preparation.ClosedMessage };

        var models = fields
            .Where(f => !preparation.FixedValues.ContainsKey(f.Alias))
            .Select(f => new SuperPowerFormFieldViewModel(
                f.Alias, f.Label, f.Type, f.Required, f.Options,
                postedValues is not null && postedValues.TryGetValue(f.Alias, out var v) ? v : "",
                errors is not null && errors.TryGetValue(f.Alias, out var e) ? e : null))
            .ToList();

        return new SuperPowerFormViewModel
        {
            FormKey = formKey,
            FormName = formName,
            Fields = models,
            SuccessMessage = submitted ? successMessage : null,
        };
    }
}

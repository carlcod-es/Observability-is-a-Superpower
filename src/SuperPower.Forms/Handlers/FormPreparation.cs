namespace SuperPower.Forms;

/// <param name="ClosedMessage">Non-null: the form is closed and this is shown instead.</param>
/// <param name="FixedValues">Field alias to fixed value; fixed fields are not rendered.</param>
public sealed record FormPreparation(string? ClosedMessage, IReadOnlyDictionary<string, string> FixedValues)
{
    public static FormPreparation Open { get; } = new(null, new Dictionary<string, string>());

    public static FormPreparation Closed(string message) => new(message, new Dictionary<string, string>());

    public static FormPreparation WithFixedValues(IReadOnlyDictionary<string, string> fixedValues) => new(null, fixedValues);

    public static FormPreparation WithFixedValues(params (string Alias, string Value)[] values) =>
        new(null, values.ToDictionary(v => v.Alias, v => v.Value));

    public bool IsClosed => ClosedMessage is not null;
}

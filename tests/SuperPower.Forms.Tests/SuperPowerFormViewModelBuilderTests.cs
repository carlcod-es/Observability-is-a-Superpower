using Xunit;

namespace SuperPower.Forms.Tests;

public class SuperPowerFormViewModelBuilderTests
{
    private static readonly Guid Key = Guid.NewGuid();

    private static readonly FormFieldDefinition[] Fields =
    [
        new("name", "Name", FormFieldType.Text) { Required = true },
        new("event", "Event", FormFieldType.Text),
        new("size", "Size", FormFieldType.Select) { Options = ["S", "M"] },
    ];

    private static SuperPowerFormViewModel Build(
        FormPreparation? prep = null,
        Dictionary<string, string>? posted = null,
        Dictionary<string, string>? errors = null,
        bool submitted = false) =>
        SuperPowerFormViewModelBuilder.Build(Key, "Contact", Fields, prep ?? FormPreparation.Open, posted, errors, "Thanks!", submitted);

    [Fact]
    public void Open_form_exposes_all_fields_in_order_with_options()
    {
        var m = Build();
        Assert.Equal(["name", "event", "size"], m.Fields.Select(f => f.Alias));
        Assert.Equal(["S", "M"], m.Fields[2].Options);
        Assert.True(m.Fields[0].Required);
        Assert.Null(m.ClosedMessage);
        Assert.Equal("", m.Fields[0].Value);
        Assert.Null(m.Fields[0].Error);
    }

    [Fact]
    public void Fixed_fields_are_omitted()
    {
        var m = Build(FormPreparation.WithFixedValues(("event", "e1")));
        Assert.Equal(["name", "size"], m.Fields.Select(f => f.Alias));
    }

    [Fact]
    public void Closed_form_exposes_only_its_message()
    {
        var m = Build(FormPreparation.Closed("Sold out"), submitted: true);
        Assert.Equal("Sold out", m.ClosedMessage);
        Assert.Empty(m.Fields);
        Assert.Null(m.SuccessMessage);
    }

    [Fact]
    public void Posted_values_and_errors_are_carried_by_alias()
    {
        var m = Build(posted: new() { ["name"] = "Ann", ["unknown"] = "x" }, errors: new() { ["size"] = "Required" });
        Assert.Equal("Ann", m.Fields[0].Value);
        Assert.Null(m.Fields[0].Error);
        Assert.Equal("Required", m.Fields[2].Error);
        Assert.Equal("", m.Fields[2].Value);
    }

    [Fact]
    public void Success_message_only_when_submitted()
    {
        Assert.Null(Build(submitted: false).SuccessMessage);
        Assert.Equal("Thanks!", Build(submitted: true).SuccessMessage);
    }

    [Fact]
    public void Post_target_and_identity()
    {
        var m = Build();
        Assert.Equal("SuperPowerForm", m.ControllerName);
        Assert.Equal("Submit", m.ActionName);
        Assert.Equal(Key, m.FormKey);
        Assert.Equal("Contact", m.FormName);
    }
}

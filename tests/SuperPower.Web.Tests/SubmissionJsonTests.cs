using SuperPower.Web.Forms;
using SuperPower.Web.Forms.Storage;
using Xunit;

namespace SuperPower.Web.Tests;

public class SubmissionJsonTests
{
    private static string[] Keys(object model) => SubmissionJson.ToFields(model).Select(f => f.Key).ToArray();

    [Fact]
    public void Field_order_matches_declaration_order()
    {
        Assert.Equal(["Name", "Email", "Message"], Keys(new ContactForm()));
        Assert.Equal(["Name", "Email", "Company", "Website", "Message"], Keys(new ServiceEnquiryForm()));
        Assert.Equal(["Name", "Email", "Notes", "JoinMailingList"], Keys(new EventBookingForm()));
    }

    [Fact]
    public void Bools_map_to_true_and_false()
    {
        var yes = SubmissionJson.ToFields(new EventBookingForm { JoinMailingList = true });
        var no = SubmissionJson.ToFields(new EventBookingForm { JoinMailingList = false });

        Assert.Equal("true", yes.Single(f => f.Key == "JoinMailingList").Value);
        Assert.Equal("false", no.Single(f => f.Key == "JoinMailingList").Value);
    }

    [Fact]
    public void Null_values_map_to_empty_string()
    {
        var fields = SubmissionJson.ToFields(new ServiceEnquiryForm { Name = "Ann" });

        Assert.Equal("Ann", fields[0].Value);
        Assert.All(fields.Skip(1), f => Assert.Equal("", f.Value));
    }

    [Fact]
    public void Serialize_pins_relaxed_encoder_output()
    {
        var form = new ContactForm { Name = "A \"q\" <b>", Email = "a@example.com", Message = "line1\nå中" };

        var json = SubmissionJson.Serialize(SubmissionJson.ToFields(form));

        Assert.Equal(
            "{\"Name\":\"A \\\"q\\\" <b>\",\"Email\":\"a@example.com\",\"Message\":\"line1\\nå中\"}",
            json);
    }
}

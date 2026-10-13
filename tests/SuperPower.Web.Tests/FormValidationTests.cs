using System.ComponentModel.DataAnnotations;
using SuperPower.Web.Forms;
using Xunit;

namespace SuperPower.Web.Tests;

public class FormValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static IEnumerable<string> Members(List<ValidationResult> results) =>
        results.SelectMany(r => r.MemberNames);

    private static ContactForm Contact() => new() { Name = "Ann", Email = "ann@example.com", Message = "Hi" };

    private static ServiceEnquiryForm Enquiry() => new() { Name = "Ann", Email = "ann@example.com", Message = "Hi" };

    private static EventBookingForm Booking() => new() { Name = "Ann", Email = "ann@example.com" };

    [Fact]
    public void Valid_models_pass()
    {
        Assert.Empty(Validate(Contact()));
        Assert.Empty(Validate(Enquiry()));
        Assert.Empty(Validate(Booking()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Contact_required_fields_fail_when_empty(string? empty)
    {
        var form = Contact();
        form.Name = empty;
        form.Email = empty;
        form.Message = empty;
        Assert.Equivalent(new[] { "Name", "Email", "Message" }, Members(Validate(form)).Distinct().ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ServiceEnquiry_required_fields_fail_when_empty(string? empty)
    {
        var form = Enquiry();
        form.Name = empty;
        form.Email = empty;
        form.Message = empty;
        Assert.Equivalent(new[] { "Name", "Email", "Message" }, Members(Validate(form)).Distinct().ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EventBooking_required_fields_fail_when_empty(string? empty)
    {
        var form = Booking();
        form.Name = empty;
        form.Email = empty;
        Assert.Equivalent(new[] { "Name", "Email" }, Members(Validate(form)).Distinct().ToArray());
    }

    [Fact]
    public void Invalid_email_fails_on_every_form()
    {
        var contact = Contact();
        contact.Email = "not-an-email";
        var enquiry = Enquiry();
        enquiry.Email = "not-an-email";
        var booking = Booking();
        booking.Email = "not-an-email";

        Assert.Equal(["Email"], Members(Validate(contact)));
        Assert.Equal(["Email"], Members(Validate(enquiry)));
        Assert.Equal(["Email"], Members(Validate(booking)));
    }

    [Fact]
    public void Optional_fields_may_be_empty()
    {
        var enquiry = Enquiry();
        enquiry.Company = "";
        enquiry.Website = null;
        var booking = Booking();
        booking.Notes = "";

        Assert.Empty(Validate(enquiry));
        Assert.Empty(Validate(booking));
    }

    [Fact]
    public void Website_accepts_non_url_text()
    {
        var enquiry = Enquiry();
        enquiry.Website = "not a url at all";
        Assert.Empty(Validate(enquiry));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Checkboxes_never_fail_validation(bool value)
    {
        var contact = Contact();
        contact.ReplyByPhone = value;
        var booking = Booking();
        booking.JoinMailingList = value;

        Assert.Empty(Validate(contact));
        Assert.Empty(Validate(booking));
    }
}

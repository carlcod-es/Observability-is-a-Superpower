using SuperPower.Web.Forms;
using Xunit;

namespace SuperPower.Web.Tests;

public class SubmissionNotificationTests
{
    [Theory]
    [InlineData(42, "ACME-0042")]
    [InlineData(1, "ACME-0001")]
    [InlineData(12345, "ACME-12345")]
    public void Reference_is_padded_to_four_digits(int n, string expected) =>
        Assert.Equal(expected, SubmissionReference.Format(n));

    [Fact]
    public void Build_names_form_page_reference_time_and_fields()
    {
        var form = new ServiceEnquiryForm { Name = "Ann", Email = "ann@example.com", Message = "Hi" };

        var (subject, body) = SubmissionNotification.Build(
            "Service enquiry", "Web design", "ACME-0042", new DateTime(2026, 10, 16, 13, 30, 5, DateTimeKind.Utc),
            SubmissionNotification.LabelledFields(form));

        Assert.Equal("New Service enquiry submission ACME-0042", subject);
        Assert.Contains("Service enquiry", body);
        Assert.Contains("\"Web design\"", body);
        Assert.Contains("Reference: ACME-0042", body);
        Assert.Contains("Submitted (UTC): 2026-10-16 13:30:05", body);
        Assert.Contains("Name: Ann", body);
        Assert.Contains("Email: ann@example.com", body);
        Assert.Contains("Message: Hi", body);
    }

    [Fact]
    public void Labels_come_from_display_names()
    {
        var fields = SubmissionNotification.LabelledFields(new EventBookingForm { JoinMailingList = true });

        Assert.Equal(["Name", "Email", "Notes", "Join mailing list"], fields.Select(f => f.Key).ToArray());
        Assert.Equal("true", fields[3].Value);
    }
}

using System.ComponentModel.DataAnnotations;

namespace SuperPower.Web.Forms;

public class ServiceEnquiryForm
{
    [Required, Display(Name = "Name")]
    public string? Name { get; set; }

    [Required, EmailAddress, Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Company")]
    public string? Company { get; set; }

    [Display(Name = "Website")]
    public string? Website { get; set; }

    [Required, DataType(DataType.MultilineText), Display(Name = "Message")]
    public string? Message { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SuperPower.Web.Forms;

public class ContactForm
{
    [Required, Display(Name = "Name")]
    public string? Name { get; set; }

    [Required, EmailAddress, Display(Name = "Email")]
    public string? Email { get; set; }

    [Required, DataType(DataType.MultilineText), Display(Name = "Message")]
    public string? Message { get; set; }
}

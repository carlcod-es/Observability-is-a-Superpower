using System.ComponentModel.DataAnnotations;

namespace SuperPower.Web.Forms;

public class EventBookingForm
{
    [Required, Display(Name = "Name")]
    public string? Name { get; set; }

    [Required, EmailAddress, Display(Name = "Email")]
    public string? Email { get; set; }

    [DataType(DataType.MultilineText), Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Join mailing list")]
    public bool JoinMailingList { get; set; }
}

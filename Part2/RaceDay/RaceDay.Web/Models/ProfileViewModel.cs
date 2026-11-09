using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// The Participant's profile: details they can edit and details shown for reference
public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your first name.")]
    [MaxLength(50, ErrorMessage = "Your first name can be at most 50 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [MaxLength(50, ErrorMessage = "Your last name can be at most 50 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "That phone number is too long.")]
    [RegularExpression(@"^[0-9+()\-\s]*$",
        ErrorMessage = "A phone number can only contain digits, spaces and + ( ) -")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    // Shown but not editable
    public string? Email { get; set; }

    public string? ProfilePictureUrl { get; set; }

    // Letters shown on the avatar when there is no picture
    public string Initials
    {
        get
        {
            var first = string.IsNullOrWhiteSpace(FirstName) ? "" : FirstName.Trim()[..1];
            var last = string.IsNullOrWhiteSpace(LastName) ? "" : LastName.Trim()[..1];
            var initials = (first + last).ToUpperInvariant();
            return initials.Length > 0 ? initials : "?";
        }
    }
}
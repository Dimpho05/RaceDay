using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your first name.")]
    [MaxLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [MaxLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [MinLength(8, ErrorMessage = "Your password must be at least 8 characters long.")]
    [MaxLength(100)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please tell us how you will use RaceDay.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Participant or Organiser.")]
    [Display(Name = "I want to")]
    public string Role { get; set; } = "Participant";

    [MaxLength(20, ErrorMessage = "That phone number is too long.")]
    [Display(Name = "Phone number (optional)")]
    public string? PhoneNumber { get; set; }
}
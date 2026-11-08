using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

/// <summary>
/// The new status for an enrolment.
/// </summary>
public class UpdateEnrolmentStatusRequest
{
    /// <summary>Pending, Confirmed or Cancelled.</summary>
    [Required]
    [RegularExpression("^(Pending|Confirmed|Cancelled)$",
        ErrorMessage = "The status must be Pending, Confirmed or Cancelled.")]
    public string Status { get; set; } = string.Empty;
}
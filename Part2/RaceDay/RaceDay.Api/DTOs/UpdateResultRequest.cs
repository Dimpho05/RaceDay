using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class UpdateResultRequest
{
    // Finish time as hours:minutes:seconds, e.g. "01:38:42"
    [Required]
    [RegularExpression(@"^\d{1,2}:\d{2}:\d{2}$",
        ErrorMessage = "Finish time must be in the format HH:MM:SS, e.g. 01:38:42.")]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int FinishingPosition { get; set; }
}
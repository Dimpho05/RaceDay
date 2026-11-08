using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// One enrolled participant and their result (if one has been recorded yet)
public class ResultRow
{
    public EventEnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

// Everything the Organiser's Results page shows
public class ResultsEntryViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();

    // Results can only be entered once the event has taken place
    public bool EventHasFinished { get; set; }

    public int ResultsRecorded => Rows.Count(r => r.Result != null);
}

// What the Organiser types for one participant's result
public class SaveResultViewModel
{
    public int EventId { get; set; }

    public int EnrolmentId { get; set; }

    // 0 when no result exists yet (a new result); otherwise the result being updated
    public int ResultId { get; set; }

    [Required(ErrorMessage = "Please enter the finish time.")]
    [RegularExpression(@"^\d{1,2}:\d{2}:\d{2}$",
        ErrorMessage = "The finish time must look like 01:38:42 (hours:minutes:seconds).")]
    [Display(Name = "Finish time")]
    public string FinishTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the finishing position.")]
    [Range(1, 100000, ErrorMessage = "The finishing position must be 1 or higher.")]
    [Display(Name = "Finishing position")]
    public int? FinishingPosition { get; set; }
}
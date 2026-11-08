namespace RaceDay.Web.Models;

// One recorded result, as returned by the API
public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    // Hours:minutes:seconds, e.g. "01:38:42"
    public string FinishTime { get; set; } = string.Empty;

    public int FinishingPosition { get; set; }
    public DateTime ResultDate { get; set; }
}
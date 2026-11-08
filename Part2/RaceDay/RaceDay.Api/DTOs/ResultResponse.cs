namespace RaceDay.Api.DTOs;

public class ResultResponse
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string FinishTime { get; set; } = string.Empty;
    public int FinishingPosition { get; set; }

    // How many participants have a recorded result in this event (for "47th out of 312")
    public int TotalFinishers { get; set; }
    public DateTime ResultDate { get; set; }
}
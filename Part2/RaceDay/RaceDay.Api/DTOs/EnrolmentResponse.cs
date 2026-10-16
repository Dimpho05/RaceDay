namespace RaceDay.Api.DTOs;

public class EnrolmentResponse
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string EnrolmentStatus { get; set; } = string.Empty;
}
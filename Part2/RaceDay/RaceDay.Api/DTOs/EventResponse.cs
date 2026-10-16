namespace RaceDay.Api.DTOs;

public class EventResponse
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public int EventTypeID { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public int OrganiserID { get; set; }
    public DateTime CreateAt { get; set; }
}
namespace RaceDay.Web.Models;

// One kind of event (Run, Walk, Cycle) as returned by the API
public class EventTypeDto
{
    public int EventTypeID { get; set; }
    public string TypeName { get; set; } = string.Empty;
}
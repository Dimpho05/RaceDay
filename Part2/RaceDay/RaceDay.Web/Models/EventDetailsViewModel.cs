namespace RaceDay.Web.Models;

// The event detail page: the event and the categories a participant can choose from
public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();

    // An event that has already taken place cannot be entered
    public bool HasFinished => Event.EventDate < DateTime.Now;
}
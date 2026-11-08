namespace RaceDay.Web.Models;

// The event detail page: the event, its categories and (for a Participant) their enrolment
public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();

    // True when the logged-in user is a Participant (only Participants can enter events)
    public bool IsParticipant { get; set; }

    // The Participant's enrolment in this event, or null if they have not entered yet
    public EnrolmentDto? MyEnrolment { get; set; }

    // An event that has already taken place cannot be entered
    public bool HasFinished => Event.EventDate < DateTime.Now;

    public bool CanEnrol => IsParticipant && !HasFinished && MyEnrolment == null && Categories.Any();
}
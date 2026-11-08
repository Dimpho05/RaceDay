namespace RaceDay.Web.Models;

// One of the participant's enrolments together with the event it is for
public class MyEnrolmentRow
{
    public EnrolmentDto Enrolment { get; set; } = new();

    // Null if the event details could not be loaded
    public EventDto? Event { get; set; }
}

public class MyEnrolmentsViewModel
{
    // Events that have not happened yet (soonest first)
    public List<MyEnrolmentRow> Upcoming { get; set; } = new();

    // Events that have already taken place (most recent first)
    public List<MyEnrolmentRow> Past { get; set; } = new();

    public int Total => Upcoming.Count + Past.Count;

    public string? ErrorMessage { get; set; }
}
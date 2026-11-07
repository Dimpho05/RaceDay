namespace RaceDay.Web.Models;

public class DashboardEventRow
{
    public EventDto Event { get; set; } = new();

    // Null when the enrolments could not be loaded
    public int? EnrolmentCount { get; set; }

    public bool IsUpcoming => Event.EventDate.Date >= DateTime.Today;
}

public class DashboardViewModel
{
    public List<DashboardEventRow> Events { get; set; } = new();

    // Shown to the user if something could not be loaded
    public string? ErrorMessage { get; set; }

    public int TotalEvents => Events.Count;
    public int UpcomingEvents => Events.Count(e => e.IsUpcoming);
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount ?? 0);

    // The soonest event that has not happened yet
    public DashboardEventRow? NextEvent => Events
        .Where(e => e.IsUpcoming)
        .OrderBy(e => e.Event.EventDate)
        .FirstOrDefault();
}
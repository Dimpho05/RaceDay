namespace RaceDay.Web.Models;

// How many participants are enrolled in one category (for the filter dropdown)
public class CategoryCount
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

// Everything the Organiser's Enrolments page shows
public class EnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();

    // The rows to show (already filtered by category if one is selected)
    public List<EventEnrolmentDto> Enrolments { get; set; } = new();

    // Enrolments in the event before any filter is applied
    public int TotalCount { get; set; }

    // Totals by status, also before any filter is applied
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }

    public List<CategoryCount> Categories { get; set; } = new();

    public int? SelectedCategoryId { get; set; }
}
namespace RaceDay.Web.Models;

// The Participant home page: upcoming events plus the filters the user chose
public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new();

    // Options for the event type filter
    public List<EventTypeDto> EventTypes { get; set; } = new();

    // The filters currently applied (they are kept in the form after searching)
    public string? Search { get; set; }
    public int? EventTypeId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) || EventTypeId.HasValue || FromDate.HasValue || ToDate.HasValue;

    // Shown to the user if the events could not be loaded
    public string? ErrorMessage { get; set; }
}
namespace RaceDay.Web.Models;

// One of the participant's results together with the event it belongs to
public class MyResultRow
{
    public ResultDto Result { get; set; } = new();

    // Null if the event details could not be loaded
    public EventDto? Event { get; set; }

    // The day of the race (falls back to the day the result was recorded)
    public DateTime Date => Event?.EventDate ?? Result.ResultDate;

    public bool IsPodium => Result.FinishingPosition is >= 1 and <= 3;
}

public class MyResultsViewModel
{
    // Most recent race first
    public List<MyResultRow> Rows { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public int RacesCompleted => Rows.Count;

    public int Podiums => Rows.Count(r => r.IsPodium);

    // The race where the participant finished closest to the front
    public MyResultRow? BestFinish => Rows
        .OrderBy(r => r.Result.FinishingPosition)
        .ThenByDescending(r => r.Date)
        .FirstOrDefault();
}
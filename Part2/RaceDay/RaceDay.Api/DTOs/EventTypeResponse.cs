namespace RaceDay.Api.DTOs;

/// <summary>
/// One kind of event an Organiser can create (for example Run, Walk or Cycle).
/// </summary>
public class EventTypeResponse
{
    /// <summary>The ID to send when creating or updating an event.</summary>
    public int EventTypeID { get; set; }

    /// <summary>The display name, such as "Run".</summary>
    public string TypeName { get; set; } = string.Empty;
}
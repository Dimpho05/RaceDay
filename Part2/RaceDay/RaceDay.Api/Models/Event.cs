using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Event
{
    [Key]
    public int EventID { get; set; }

    [Required, MaxLength(100)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    // Distance in km, e.g. 21.10
    [Column(TypeName = "decimal(6,2)")]
    public decimal Distance { get; set; }

    // Foreign key -> EventTypes.EventTypeID
    public int EventTypeID { get; set; }

    // Foreign key -> Users.UserID (the Organiser who owns the event)
    public int OrganiserID { get; set; }

    public DateTime CreateAt { get; set; } = DateTime.Now;
}
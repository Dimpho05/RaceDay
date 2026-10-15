using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class EventType
{
    [Key]
    public int EventTypeID { get; set; }

    // "Run", "Walk" or "Cycle"
    [Required, MaxLength(20)]
    public string TypeName { get; set; } = string.Empty;
}
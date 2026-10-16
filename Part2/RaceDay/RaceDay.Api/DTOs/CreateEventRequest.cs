using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CreateEventRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime? EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    [Range(1, int.MaxValue)]
    public int EventTypeId { get; set; }
}
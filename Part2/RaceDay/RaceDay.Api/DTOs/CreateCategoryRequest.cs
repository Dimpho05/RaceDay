using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CreateCategoryRequest
{
    [Required, MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, 120)]
    public int? MinimumAge { get; set; }

    [Range(0, 120)]
    public int? MaximumAge { get; set; }

    [Range(0.01, 1000)]
    public decimal? CategoryDistance { get; set; }
}
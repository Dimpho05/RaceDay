using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Category
{
    [Key]
    public int CategoryID { get; set; }

    // Foreign key -> Events.EventID
    public int EventID { get; set; }

    // e.g. "Under 20", "21km Open"
    [Required, MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    public int? MinimumAge { get; set; }

    public int? MaximumAge { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal? CategoryDistance { get; set; }
}
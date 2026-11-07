using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// The Add category and Edit category forms
public class CategoryFormViewModel
{
    public int CategoryID { get; set; }

    public int EventID { get; set; }

    // Shown in the page heading when editing
    public string? EventName { get; set; }

    [Required(ErrorMessage = "Please enter a name for the category.")]
    [MaxLength(50, ErrorMessage = "The category name can be at most 50 characters.")]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, 120, ErrorMessage = "The minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinimumAge { get; set; }

    [Range(0, 120, ErrorMessage = "The maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaximumAge { get; set; }

    [Range(0.01, 1000, ErrorMessage = "The distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? CategoryDistance { get; set; }
}
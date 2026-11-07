using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RaceDay.Web.Models;

// The Create event and Edit event forms
public class EventFormViewModel
{
    public int EventID { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [MaxLength(100, ErrorMessage = "The event name can be at most 100 characters.")]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "The description can be at most 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the date and time of the event.")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date and time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [MaxLength(150, ErrorMessage = "The location can be at most 150 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in kilometres.")]
    [Range(0.01, 1000, ErrorMessage = "The distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public int EventTypeId { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }

    // The banner already saved for this event (shown on the Edit form)
    public string? ExistingBannerUrl { get; set; }

    // Options for the event type dropdown; filled in by the controller
    [ValidateNever]
    public List<SelectListItem> EventTypes { get; set; } = new();
}
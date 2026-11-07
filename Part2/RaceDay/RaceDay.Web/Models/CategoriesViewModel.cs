namespace RaceDay.Web.Models;

// Everything the Categories page shows: the event, its categories and the Add form
public class CategoriesViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}
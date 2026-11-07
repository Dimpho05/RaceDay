namespace RaceDay.Web.Models;

// A category of an event (an age band and/or a distance), as returned by the API
public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public decimal? CategoryDistance { get; set; }

    // Age limits written the way a person would say them
    public string AgeRange
    {
        get
        {
            if (MinimumAge.HasValue && MaximumAge.HasValue)
            {
                return $"{MinimumAge}\u2013{MaximumAge}";
            }

            if (MinimumAge.HasValue)
            {
                return $"{MinimumAge}+";
            }

            if (MaximumAge.HasValue)
            {
                return $"Up to {MaximumAge}";
            }

            return "All ages";
        }
    }

    public string DistanceText => CategoryDistance.HasValue
        ? $"{CategoryDistance.Value:0.##} km"
        : "Event distance";
}
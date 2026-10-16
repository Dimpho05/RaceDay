using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[SessionAuthorize]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all categories defined for a specific event.
    /// </summary>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoriesForEvent(int eventId)
    {
        if (!await _context.Events.AnyAsync(e => e.EventID == eventId))
        {
            return NotFound(new { message = "Event not found." });
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.EventID == eventId)
            .Select(c => new CategoryResponse
            {
                CategoryID = c.CategoryID,
                EventID = c.EventID,
                CategoryName = c.CategoryName,
                MinimumAge = c.MinimumAge,
                MaximumAge = c.MaximumAge,
                CategoryDistance = c.CategoryDistance
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Retrieves the details of a single category by its ID.
    /// </summary>
    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => c.CategoryID == id)
            .Select(c => new CategoryResponse
            {
                CategoryID = c.CategoryID,
                EventID = c.EventID,
                CategoryName = c.CategoryName,
                MinimumAge = c.MinimumAge,
                MaximumAge = c.MaximumAge,
                CategoryDistance = c.CategoryDistance
            })
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        return Ok(category);
    }

    /// <summary>
    /// Creates a new category for an event. Only the Organiser who owns the event may add categories.
    /// </summary>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CreateCategoryRequest request)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        // Ownership check: only the Organiser who created the event may add categories
        if (ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to your own events." });
        }

        if (request.MinimumAge.HasValue && request.MaximumAge.HasValue
            && request.MinimumAge > request.MaximumAge)
        {
            return BadRequest(new { message = "Minimum age cannot be greater than maximum age." });
        }

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            MinimumAge = request.MinimumAge,
            MaximumAge = request.MaximumAge,
            CategoryDistance = request.CategoryDistance
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var response = new CategoryResponse
        {
            CategoryID = category.CategoryID,
            EventID = category.EventID,
            CategoryName = category.CategoryName,
            MinimumAge = category.MinimumAge,
            MaximumAge = category.MaximumAge,
            CategoryDistance = category.CategoryDistance
        };

        return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryID }, response);
    }

    /// <summary>
    /// Updates a category. Only the Organiser who owns the parent event may update it.
    /// </summary>
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateCategoryRequest request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        // Ownership check goes through the parent event
        var ev = await _context.Events.FindAsync(category.EventID);
        if (ev == null || ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only update categories of your own events." });
        }

        if (request.MinimumAge.HasValue && request.MaximumAge.HasValue
            && request.MinimumAge > request.MaximumAge)
        {
            return BadRequest(new { message = "Minimum age cannot be greater than maximum age." });
        }

        category.CategoryName = request.CategoryName.Trim();
        category.MinimumAge = request.MinimumAge;
        category.MaximumAge = request.MaximumAge;
        category.CategoryDistance = request.CategoryDistance;

        await _context.SaveChangesAsync();

        return Ok(new CategoryResponse
        {
            CategoryID = category.CategoryID,
            EventID = category.EventID,
            CategoryName = category.CategoryName,
            MinimumAge = category.MinimumAge,
            MaximumAge = category.MaximumAge,
            CategoryDistance = category.CategoryDistance
        });
    }

    /// <summary>
    /// Deletes a category. Only the Organiser who owns the parent event may delete it, and only while nobody is enrolled in it.
    /// </summary>
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        // Ownership check goes through the parent event
        var ev = await _context.Events.FindAsync(category.EventID);
        if (ev == null || ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories of your own events." });
        }

        // Protect participants' entries: a category with enrolments cannot be deleted
        if (await _context.Enrolments.AnyAsync(e => e.CategoryID == id))
        {
            return Conflict(new { message = "Participants have already enrolled in this category, so it cannot be deleted." });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
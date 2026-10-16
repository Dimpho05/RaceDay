using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[SessionAuthorize]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all events, soonest first. Available to any logged-in user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEvents()
    {
        var events = await (
            from e in _context.Events.AsNoTracking()
            join t in _context.EventTypes on e.EventTypeID equals t.EventTypeID
            orderby e.EventDate
            select new EventResponse
            {
                EventID = e.EventID,
                EventName = e.EventName,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                Distance = e.Distance,
                EventTypeID = e.EventTypeID,
                EventTypeName = t.TypeName,
                OrganiserID = e.OrganiserID,
                CreateAt = e.CreateAt
            }).ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Retrieves the details of a single event by its ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await (
            from e in _context.Events.AsNoTracking()
            join t in _context.EventTypes on e.EventTypeID equals t.EventTypeID
            where e.EventID == id
            select new EventResponse
            {
                EventID = e.EventID,
                EventName = e.EventName,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                Distance = e.Distance,
                EventTypeID = e.EventTypeID,
                EventTypeName = t.TypeName,
                OrganiserID = e.OrganiserID,
                CreateAt = e.CreateAt
            }).FirstOrDefaultAsync();

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(ev);
    }

    /// <summary>
    /// Creates a new event. The logged-in Organiser becomes the event's owner.
    /// </summary>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var eventType = await _context.EventTypes.FindAsync(request.EventTypeId);
        if (eventType == null)
        {
            return BadRequest(new { message = "Event type does not exist. Use 1 (Run), 2 (Walk) or 3 (Cycle)." });
        }

        var ev = new Event
        {
            EventName = request.Name.Trim(),
            Description = request.Description,
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            Distance = request.DistanceKm,
            EventTypeID = request.EventTypeId,
            OrganiserID = HttpContext.Session.GetUserId()!.Value
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        var response = new EventResponse
        {
            EventID = ev.EventID,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            EventTypeID = ev.EventTypeID,
            EventTypeName = eventType.TypeName,
            OrganiserID = ev.OrganiserID,
            CreateAt = ev.CreateAt
        };

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, response);
    }

    /// <summary>
    /// Updates an existing event. Only the Organiser who created it may update it.
    /// </summary>
    [HttpPut("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] CreateEventRequest request)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        // Ownership check: only the Organiser who created the event may change it
        if (ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only update your own events." });
        }

        var eventType = await _context.EventTypes.FindAsync(request.EventTypeId);
        if (eventType == null)
        {
            return BadRequest(new { message = "Event type does not exist. Use 1 (Run), 2 (Walk) or 3 (Cycle)." });
        }

        ev.EventName = request.Name.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.Distance = request.DistanceKm;
        ev.EventTypeID = request.EventTypeId;

        await _context.SaveChangesAsync();

        return Ok(new EventResponse
        {
            EventID = ev.EventID,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            EventTypeID = ev.EventTypeID,
            EventTypeName = eventType.TypeName,
            OrganiserID = ev.OrganiserID,
            CreateAt = ev.CreateAt
        });
    }

    /// <summary>
    /// Deletes an event. Only the Organiser who created it may delete it, and only while nobody has enrolled.
    /// </summary>
    [HttpDelete("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        // Ownership check: only the Organiser who created the event may delete it
        if (ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete your own events." });
        }

        // Protect participants' entries: an event with enrolments cannot be deleted
        var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.EventID == id);
        if (hasEnrolments)
        {
            return Conflict(new { message = "This event already has enrolments and cannot be deleted." });
        }

        // No enrolments, so remove the event's categories first, then the event itself
        var categories = _context.Categories.Where(c => c.EventID == id);
        _context.Categories.RemoveRange(categories);
        _context.Events.Remove(ev);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
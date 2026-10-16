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
}
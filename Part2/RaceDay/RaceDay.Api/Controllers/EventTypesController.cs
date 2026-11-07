using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/event-types")]
[SessionAuthorize]
public class EventTypesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventTypesController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists every event type (Run, Walk, Cycle) so a client can offer them in a dropdown.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEventTypes()
    {
        var types = await _context.EventTypes
            .AsNoTracking()
            .OrderBy(t => t.EventTypeID)
            .Select(t => new EventTypeResponse
            {
                EventTypeID = t.EventTypeID,
                TypeName = t.TypeName
            })
            .ToListAsync();

        return Ok(types);
    }
}
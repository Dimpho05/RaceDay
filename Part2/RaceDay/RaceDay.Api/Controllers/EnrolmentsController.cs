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
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enrols the logged-in Participant into an event by selecting a category.
    /// </summary>
    [HttpPost("enrolments")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEnrolment([FromBody] CreateEnrolmentRequest request)
    {
        var ev = await _context.Events.FindAsync(request.EventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        // The chosen category must belong to the chosen event
        if (category.EventID != request.EventId)
        {
            return BadRequest(new { message = "That category does not belong to this event." });
        }

        var participantId = HttpContext.Session.GetUserId()!.Value;

        // One enrolment per participant per event
        if (await _context.Enrolments.AnyAsync(e =>
                e.ParticipantID == participantId && e.EventID == request.EventId))
        {
            return Conflict(new { message = "You are already enrolled in this event." });
        }

        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            EventID = request.EventId,
            CategoryID = request.CategoryId
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        var response = new EnrolmentResponse
        {
            EnrolmentID = enrolment.EnrolmentID,
            ParticipantID = enrolment.ParticipantID,
            EventID = enrolment.EventID,
            EventName = ev.EventName,
            CategoryID = enrolment.CategoryID,
            CategoryName = category.CategoryName,
            EnrolmentDate = enrolment.EnrolmentDate,
            EnrolmentStatus = enrolment.EnrolmentStatus
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Retrieves all enrolments belonging to the logged-in Participant.
    /// </summary>
    [HttpGet("enrolments/my")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var participantId = HttpContext.Session.GetUserId()!.Value;

        var enrolments = await (
            from en in _context.Enrolments.AsNoTracking()
            join ev in _context.Events on en.EventID equals ev.EventID
            join c in _context.Categories on en.CategoryID equals c.CategoryID
            where en.ParticipantID == participantId
            orderby ev.EventDate
            select new EnrolmentResponse
            {
                EnrolmentID = en.EnrolmentID,
                ParticipantID = en.ParticipantID,
                EventID = en.EventID,
                EventName = ev.EventName,
                CategoryID = en.CategoryID,
                CategoryName = c.CategoryName,
                EnrolmentDate = en.EnrolmentDate,
                EnrolmentStatus = en.EnrolmentStatus
            }).ToListAsync();

        return Ok(enrolments);
    }
}
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

    /// <summary>
    /// Retrieves all enrolments for an event. Only the Organiser who owns the event may view them.
    /// </summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(List<EventEnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
    {
        var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        // Ownership check: only the Organiser who created the event may see its enrolments
        if (ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view enrolments for your own events." });
        }

        var enrolments = await (
            from en in _context.Enrolments.AsNoTracking()
            join u in _context.Users on en.ParticipantID equals u.UserID
            join c in _context.Categories on en.CategoryID equals c.CategoryID
            where en.EventID == eventId
            orderby en.EnrolmentDate
            select new EventEnrolmentResponse
            {
                EnrolmentID = en.EnrolmentID,
                ParticipantID = en.ParticipantID,
                ParticipantName = u.FirstName + " " + u.LastName,
                ParticipantEmail = u.Email,
                CategoryID = en.CategoryID,
                CategoryName = c.CategoryName,
                EnrolmentDate = en.EnrolmentDate,
                EnrolmentStatus = en.EnrolmentStatus
            }).ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>
    /// Cancels the logged-in Participant's own enrolment. An enrolment that already has a result cannot be cancelled.
    /// </summary>
    [HttpDelete("enrolments/{id:int}")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _context.Enrolments.FindAsync(id);
        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        // Ownership check: a Participant may only cancel their own enrolment
        if (enrolment.ParticipantID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only cancel your own enrolments." });
        }

        // A recorded result cannot be orphaned
        if (await _context.Results.AnyAsync(r => r.EnrolmentID == id))
        {
            return Conflict(new { message = "A result has already been recorded, so this enrolment cannot be cancelled." });
        }

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
[SessionAuthorize("Organiser")]
public class EnrolmentStatusController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentStatusController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Changes the status of an enrolment (Pending, Confirmed or Cancelled).
    /// Only the Organiser who owns the event can do this.
    /// </summary>
    /// <param name="id">The enrolment ID.</param>
    /// <param name="request">The new status.</param>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateEnrolmentStatusRequest request)
    {
        var enrolment = await _context.Enrolments.FindAsync(id);
        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        var ev = await _context.Events.FindAsync(enrolment.EventID);
        if (ev == null || ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change enrolments for your own events." });
        }

        enrolment.EnrolmentStatus = request.Status;
        await _context.SaveChangesAsync();

        var category = await _context.Categories.FindAsync(enrolment.CategoryID);

        return Ok(new EnrolmentResponse
        {
            EnrolmentID = enrolment.EnrolmentID,
            ParticipantID = enrolment.ParticipantID,
            EventID = enrolment.EventID,
            EventName = ev.EventName,
            CategoryID = enrolment.CategoryID,
            CategoryName = category?.CategoryName ?? string.Empty,
            EnrolmentDate = enrolment.EnrolmentDate,
            EnrolmentStatus = enrolment.EnrolmentStatus
        });
    }
}
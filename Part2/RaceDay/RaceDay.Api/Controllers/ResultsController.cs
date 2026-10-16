using System.Globalization;
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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Records a finish time and finishing position for an enrolment. Only the Organiser who owns the event may do this.
    /// </summary>
    [HttpPost("results")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateResult([FromBody] CreateResultRequest request)
    {
        if (!TimeSpan.TryParse(request.FinishTime, CultureInfo.InvariantCulture, out var finishTime)
            || finishTime <= TimeSpan.Zero)
        {
            return BadRequest(new { message = "Finish time must be a valid time such as 01:38:42." });
        }

        var enrolment = await _context.Enrolments.FindAsync(request.EnrolmentId);
        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        // Ownership check: only the Organiser who owns the event may record its results
        var ev = await _context.Events.FindAsync(enrolment.EventID);
        if (ev == null || ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only record results for your own events." });
        }

        // One result per enrolment
        if (await _context.Results.AnyAsync(r => r.EnrolmentID == request.EnrolmentId))
        {
            return Conflict(new { message = "A result has already been recorded for this enrolment." });
        }

        var result = new Result
        {
            EnrolmentID = request.EnrolmentId,
            FinishTime = finishTime,
            FinishingPosition = request.FinishingPosition
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        var participant = await _context.Users.FindAsync(enrolment.ParticipantID);
        var category = await _context.Categories.FindAsync(enrolment.CategoryID);

        var response = new ResultResponse
        {
            ResultID = result.ResultID,
            EnrolmentID = result.EnrolmentID,
            EventID = ev.EventID,
            EventName = ev.EventName,
            ParticipantID = enrolment.ParticipantID,
            ParticipantName = participant != null ? participant.FirstName + " " + participant.LastName : string.Empty,
            CategoryName = category?.CategoryName ?? string.Empty,
            FinishTime = result.FinishTime.ToString(@"hh\:mm\:ss"),
            FinishingPosition = result.FinishingPosition,
            ResultDate = result.ResultDate
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
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

        var response = await BuildResponseAsync(result, enrolment, ev);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Corrects a recorded result. Only the Organiser who owns the event may update it.
    /// </summary>
    [HttpPut("results/{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResult(int id, [FromBody] UpdateResultRequest request)
    {
        if (!TimeSpan.TryParse(request.FinishTime, CultureInfo.InvariantCulture, out var finishTime)
            || finishTime <= TimeSpan.Zero)
        {
            return BadRequest(new { message = "Finish time must be a valid time such as 01:38:42." });
        }

        var result = await _context.Results.FindAsync(id);
        if (result == null)
        {
            return NotFound(new { message = "Result not found." });
        }

        // Ownership check goes result -> enrolment -> event -> Organiser
        var enrolment = await _context.Enrolments.FindAsync(result.EnrolmentID);
        var ev = enrolment == null ? null : await _context.Events.FindAsync(enrolment.EventID);
        if (enrolment == null || ev == null || ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only update results for your own events." });
        }

        result.FinishTime = finishTime;
        result.FinishingPosition = request.FinishingPosition;

        await _context.SaveChangesAsync();

        var response = await BuildResponseAsync(result, enrolment, ev);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves all results belonging to the logged-in Participant, newest event first.
    /// </summary>
    [HttpGet("results/my")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        var participantId = HttpContext.Session.GetUserId()!.Value;

        var rows = await QueryResults()
            .Where(x => x.Enrolment.ParticipantID == participantId)
            .OrderByDescending(x => x.Event.EventDate)
            .ToListAsync();

        return Ok(rows.Select(ToResponse).ToList());
    }

    /// <summary>
    /// Retrieves all results for an event, ordered by finishing position. Viewable by the owning Organiser and by Participants enrolled in the event.
    /// </summary>
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResultsForEvent(int eventId)
    {
        var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        var userId = HttpContext.Session.GetUserId()!.Value;
        var role = HttpContext.Session.GetRole();

        var allowed = role == "Organiser"
            ? ev.OrganiserID == userId
            : await _context.Enrolments.AnyAsync(e => e.EventID == eventId && e.ParticipantID == userId);

        if (!allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only the owning Organiser or an enrolled Participant can view these results." });
        }

        var rows = await QueryResults()
            .Where(x => x.Event.EventID == eventId)
            .OrderBy(x => x.Result.FinishingPosition)
            .ToListAsync();

        return Ok(rows.Select(ToResponse).ToList());
    }

    // Joins a result to its enrolment, event, participant and category in one query
    private IQueryable<ResultRow> QueryResults()
    {
        return from r in _context.Results.AsNoTracking()
               join en in _context.Enrolments on r.EnrolmentID equals en.EnrolmentID
               join ev in _context.Events on en.EventID equals ev.EventID
               join u in _context.Users on en.ParticipantID equals u.UserID
               join c in _context.Categories on en.CategoryID equals c.CategoryID
               select new ResultRow { Result = r, Enrolment = en, Event = ev, User = u, Category = c };
    }

    private static ResultResponse ToResponse(ResultRow x) => new ResultResponse
    {
        ResultID = x.Result.ResultID,
        EnrolmentID = x.Result.EnrolmentID,
        EventID = x.Event.EventID,
        EventName = x.Event.EventName,
        ParticipantID = x.Enrolment.ParticipantID,
        ParticipantName = x.User.FirstName + " " + x.User.LastName,
        CategoryName = x.Category.CategoryName,
        FinishTime = x.Result.FinishTime.ToString(@"hh\:mm\:ss"),
        FinishingPosition = x.Result.FinishingPosition,
        ResultDate = x.Result.ResultDate
    };

    private class ResultRow
    {
        public Result Result { get; set; } = null!;
        public Enrolment Enrolment { get; set; } = null!;
        public Event Event { get; set; } = null!;
        public User User { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }

    // Builds the readable response (names instead of bare IDs) for a result
    private async Task<ResultResponse> BuildResponseAsync(Result result, Enrolment enrolment, Event ev)
    {
        var participant = await _context.Users.FindAsync(enrolment.ParticipantID);
        var category = await _context.Categories.FindAsync(enrolment.CategoryID);

        return new ResultResponse
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
    }
}
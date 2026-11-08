using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class ManageResultsController : AppControllerBase
{
    private readonly IRaceDayApiClient _api;

    public ManageResultsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // ---------- List of participants with their results ----------

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var eventCheck = await LoadOwnEventAsync(eventId);
        if (eventCheck.Redirect != null)
        {
            return eventCheck.Redirect;
        }

        var enrolmentsResult = await _api.GetAsync<List<EventEnrolmentDto>>($"api/events/{eventId}/enrolments");
        var expired = RedirectIfSessionExpired(enrolmentsResult);
        if (expired != null)
        {
            return expired;
        }

        var resultsResult = await _api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results");
        expired = RedirectIfSessionExpired(resultsResult);
        if (expired != null)
        {
            return expired;
        }

        if (!enrolmentsResult.Success)
        {
            ViewData["ErrorMessage"] = enrolmentsResult.ErrorMessage;
        }
        else if (!resultsResult.Success)
        {
            ViewData["ErrorMessage"] = resultsResult.ErrorMessage;
        }

        var enrolments = enrolmentsResult.Data ?? new List<EventEnrolmentDto>();
        var results = resultsResult.Data ?? new List<ResultDto>();

        // Match each enrolment with its result (one result per enrolment)
        var rows = enrolments
            .Select(e => new ResultRow
            {
                Enrolment = e,
                Result = results.FirstOrDefault(r => r.EnrolmentID == e.EnrolmentID)
            })
            .ToList();

        // Participants with results first (by finishing position), then those still waiting
        var ordered = rows.Where(r => r.Result != null).OrderBy(r => r.Result!.FinishingPosition)
            .Concat(rows.Where(r => r.Result == null).OrderBy(r => r.Enrolment.ParticipantName))
            .ToList();

        return View(new ResultsEntryViewModel
        {
            Event = eventCheck.Event!,
            Rows = ordered,
            EventHasFinished = eventCheck.Event!.EventDate <= DateTime.Now
        });
    }

    // ---------- Save (create or update) one result ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SaveResultViewModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? "Please check the finish time and the finishing position.";
            return RedirectToAction(nameof(Index), new { eventId = input.EventId });
        }

        // Re-check on the server that this is the Organiser's own event and that it has taken place
        var eventCheck = await LoadOwnEventAsync(input.EventId);
        if (eventCheck.Redirect != null)
        {
            return eventCheck.Redirect;
        }

        if (eventCheck.Event!.EventDate > DateTime.Now)
        {
            TempData["Error"] = "Results can only be entered after the event has taken place.";
            return RedirectToAction(nameof(Index), new { eventId = input.EventId });
        }

        ApiResult result;

        if (input.ResultId > 0)
        {
            result = await _api.PutAsync($"api/results/{input.ResultId}", new
            {
                finishTime = input.FinishTime,
                finishingPosition = input.FinishingPosition
            });
        }
        else
        {
            result = await _api.PostAsync("api/results", new
            {
                enrolmentId = input.EnrolmentId,
                finishTime = input.FinishTime,
                finishingPosition = input.FinishingPosition
            });
        }

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (result.Success)
        {
            TempData["Success"] = "The result was saved and is now visible to the participant.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index), new { eventId = input.EventId });
    }

    // ---------- Helpers ----------

    // Loads the event and confirms it belongs to the logged-in Organiser
    private async Task<(IActionResult? Redirect, EventDto? Event)> LoadOwnEventAsync(int eventId)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{eventId}");

        var expired = RedirectIfSessionExpired(eventResult);
        if (expired != null)
        {
            return (expired, null);
        }

        if (!eventResult.Success || eventResult.Data == null)
        {
            TempData["Error"] = eventResult.IsNotFound
                ? "That event could not be found."
                : eventResult.ErrorMessage;
            return (RedirectToAction("Index", "ManageEvents"), null);
        }

        if (eventResult.Data.OrganiserID != HttpContext.Session.GetUserId())
        {
            TempData["Error"] = "You can only enter results for your own events.";
            return (RedirectToAction("Index", "ManageEvents"), null);
        }

        return (null, eventResult.Data);
    }
}
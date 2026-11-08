using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Participant")]
public class MyEnrolmentsController : AppControllerBase
{
    private readonly IRaceDayApiClient _api;

    public MyEnrolmentsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // Every event the logged-in Participant has entered, with its status
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var enrolmentsResult = await _api.GetAsync<List<EnrolmentDto>>("api/enrolments/my");

        var expired = RedirectIfSessionExpired(enrolmentsResult);
        if (expired != null)
        {
            return expired;
        }

        var model = new MyEnrolmentsViewModel();

        if (!enrolmentsResult.Success || enrolmentsResult.Data == null)
        {
            model.ErrorMessage = enrolmentsResult.ErrorMessage;
            return View(model);
        }

        // The enrolment only holds the event name, so fetch the events for dates, places and banners
        var eventsResult = await _api.GetAsync<List<EventDto>>("api/events");

        expired = RedirectIfSessionExpired(eventsResult);
        if (expired != null)
        {
            return expired;
        }

        var eventsById = (eventsResult.Data ?? new List<EventDto>()).ToDictionary(e => e.EventID);

        var rows = enrolmentsResult.Data
            .Select(e => new MyEnrolmentRow
            {
                Enrolment = e,
                Event = eventsById.GetValueOrDefault(e.EventID)
            })
            .ToList();

        model.Upcoming = rows
            .Where(r => r.Event == null || r.Event.EventDate >= DateTime.Now)
            .OrderBy(r => r.Event?.EventDate ?? DateTime.MaxValue)
            .ToList();

        model.Past = rows
            .Where(r => r.Event != null && r.Event.EventDate < DateTime.Now)
            .OrderByDescending(r => r.Event!.EventDate)
            .ToList();

        return View(model);
    }

    // Withdraw from an event
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await _api.DeleteAsync($"api/enrolments/{id}");

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (result.Success)
        {
            TempData["Success"] = "You have withdrawn from the event.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index));
    }
}
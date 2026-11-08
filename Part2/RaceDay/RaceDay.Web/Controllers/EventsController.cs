using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Event pages for logged-in users (Participants browse and enter events here)
[RoleAuthorize]
public class EventsController : AppControllerBase
{
    private readonly IRaceDayApiClient _api;

    public EventsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // Full details of one event, its categories and (for Participants) their enrolment
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{id}");

        var expired = RedirectIfSessionExpired(eventResult);
        if (expired != null)
        {
            return expired;
        }

        if (!eventResult.Success || eventResult.Data == null)
        {
            TempData["Error"] = eventResult.IsNotFound
                ? "That event could not be found."
                : eventResult.ErrorMessage;
            return RedirectToUserHome();
        }

        var categoriesResult = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");

        expired = RedirectIfSessionExpired(categoriesResult);
        if (expired != null)
        {
            return expired;
        }

        if (!categoriesResult.Success)
        {
            ViewData["ErrorMessage"] = categoriesResult.ErrorMessage;
        }

        var model = new EventDetailsViewModel
        {
            Event = eventResult.Data,
            Categories = categoriesResult.Data ?? new List<CategoryDto>(),
            IsParticipant = HttpContext.Session.GetRole() == "Participant"
        };

        // A Participant may already be entered in this event
        if (model.IsParticipant)
        {
            var mine = await _api.GetAsync<List<EnrolmentDto>>("api/enrolments/my");

            expired = RedirectIfSessionExpired(mine);
            if (expired != null)
            {
                return expired;
            }

            model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventID == id);
        }

        return View(model);
    }

    // Enter an event in the chosen category
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RoleAuthorize("Participant")]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (!categoryId.HasValue)
        {
            TempData["Error"] = "Please choose a category before entering the event.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await _api.PostAsync<EnrolmentDto>("api/enrolments", new
        {
            eventId,
            categoryId = categoryId.Value
        });

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (!result.Success)
        {
            // For example: "You are already enrolled in this event."
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = "You are entered! Your enrolment is pending until the organiser confirms it.";
        return RedirectToAction("Index", "MyEnrolments");
    }
}
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

    // Full details of one event and the categories on offer
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

        return View(new EventDetailsViewModel
        {
            Event = eventResult.Data,
            Categories = categoriesResult.Data ?? new List<CategoryDto>()
        });
    }
}
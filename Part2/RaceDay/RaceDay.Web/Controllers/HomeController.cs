using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppControllerBase
{
    private readonly IRaceDayApiClient _api;

    public HomeController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // Visitors see a welcome banner; Participants see upcoming events with filters;
    // Organisers are sent to their dashboard
    public async Task<IActionResult> Index(string? search, int? eventTypeId, DateTime? fromDate, DateTime? toDate)
    {
        var role = HttpContext.Session.GetRole();

        if (role == "Organiser")
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var model = new HomeViewModel
        {
            Search = search,
            EventTypeId = eventTypeId,
            FromDate = fromDate,
            ToDate = toDate
        };

        if (role == null)
        {
            return View(model);
        }

        // Ask the API for upcoming events that match the filters
        var query = new List<string> { "upcomingOnly=true" };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add("search=" + Uri.EscapeDataString(search.Trim()));
        }

        if (eventTypeId.HasValue)
        {
            query.Add("eventTypeId=" + eventTypeId.Value);
        }

        if (fromDate.HasValue)
        {
            query.Add("fromDate=" + fromDate.Value.ToString("yyyy-MM-dd"));
        }

        if (toDate.HasValue)
        {
            query.Add("toDate=" + toDate.Value.ToString("yyyy-MM-dd"));
        }

        var eventsResult = await _api.GetAsync<List<EventDto>>("api/events?" + string.Join("&", query));

        var expired = RedirectIfSessionExpired(eventsResult);
        if (expired != null)
        {
            return expired;
        }

        if (eventsResult.Success && eventsResult.Data != null)
        {
            model.Events = eventsResult.Data;
        }
        else
        {
            model.ErrorMessage = eventsResult.ErrorMessage;
        }

        var typesResult = await _api.GetAsync<List<EventTypeDto>>("api/event-types");
        if (typesResult.Success && typesResult.Data != null)
        {
            model.EventTypes = typesResult.Data;
        }

        return View(model);
    }

    // Shown for unexpected errors; never displays technical details
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Shown for 404 and other status codes (see UseStatusCodePagesWithReExecute in Program.cs)
    public IActionResult HttpStatus(int code)
    {
        return View("StatusCode", code);
    }
}
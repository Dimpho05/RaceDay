using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Participant")]
public class MyResultsController : AppControllerBase
{
    private readonly IRaceDayApiClient _api;

    public MyResultsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // The logged-in Participant's race history: every event with a recorded result
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var resultsResult = await _api.GetAsync<List<ResultDto>>("api/results/my");

        var expired = RedirectIfSessionExpired(resultsResult);
        if (expired != null)
        {
            return expired;
        }

        var model = new MyResultsViewModel();

        if (!resultsResult.Success || resultsResult.Data == null)
        {
            model.ErrorMessage = resultsResult.ErrorMessage;
            return View(model);
        }

        // A result does not carry the race date, so fetch the events to show it
        var eventsResult = await _api.GetAsync<List<EventDto>>("api/events");

        expired = RedirectIfSessionExpired(eventsResult);
        if (expired != null)
        {
            return expired;
        }

        var eventsById = (eventsResult.Data ?? new List<EventDto>()).ToDictionary(e => e.EventID);

        model.Rows = resultsResult.Data
            .Select(r => new MyResultRow
            {
                Result = r,
                Event = eventsById.GetValueOrDefault(r.EventID)
            })
            .OrderByDescending(r => r.Date)
            .ToList();

        return View(model);
    }
}
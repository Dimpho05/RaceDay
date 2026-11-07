using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class DashboardController : AppControllerBase
{
    private readonly IRaceDayApiClient _api;

    public DashboardController(IRaceDayApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();

        var eventsResult = await _api.GetAsync<List<EventDto>>("api/events");

        var expired = RedirectIfSessionExpired(eventsResult);
        if (expired != null)
        {
            return expired;
        }

        if (!eventsResult.Success || eventsResult.Data == null)
        {
            model.ErrorMessage = eventsResult.ErrorMessage;
            return View(model);
        }

        // The API lists every event; the dashboard shows only this Organiser's own events
        var userId = HttpContext.Session.GetUserId();
        var mine = eventsResult.Data.Where(e => e.OrganiserID == userId).ToList();

        // Upcoming events first (soonest at the top), then past events (most recent first)
        var ordered = mine.Where(e => e.EventDate.Date >= DateTime.Today).OrderBy(e => e.EventDate)
            .Concat(mine.Where(e => e.EventDate.Date < DateTime.Today).OrderByDescending(e => e.EventDate));

        var enrolmentsMissing = false;

        foreach (var ev in ordered)
        {
            var enrolments = await _api.GetAsync<List<EventEnrolmentDto>>($"api/events/{ev.EventID}/enrolments");

            var expiredNow = RedirectIfSessionExpired(enrolments);
            if (expiredNow != null)
            {
                return expiredNow;
            }

            if (!enrolments.Success)
            {
                enrolmentsMissing = true;
            }

            model.Events.Add(new DashboardEventRow
            {
                Event = ev,
                EnrolmentCount = enrolments.Success ? enrolments.Data?.Count ?? 0 : null
            });
        }

        if (enrolmentsMissing)
        {
            model.ErrorMessage = "Some enrolment totals could not be loaded right now.";
        }

        return View(model);
    }
}
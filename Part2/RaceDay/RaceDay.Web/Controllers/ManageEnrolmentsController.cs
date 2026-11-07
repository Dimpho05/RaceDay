using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class ManageEnrolmentsController : AppControllerBase
{
    private readonly IRaceDayApiClient _api;

    public ManageEnrolmentsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // Lists everyone enrolled in one event, optionally filtered by category
    [HttpGet]
    public async Task<IActionResult> Index(int eventId, int? categoryId)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{eventId}");

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
            return RedirectToAction("Index", "ManageEvents");
        }

        if (eventResult.Data.OrganiserID != HttpContext.Session.GetUserId())
        {
            TempData["Error"] = "You can only view enrolments for your own events.";
            return RedirectToAction("Index", "ManageEvents");
        }

        var enrolmentsResult = await _api.GetAsync<List<EventEnrolmentDto>>($"api/events/{eventId}/enrolments");

        expired = RedirectIfSessionExpired(enrolmentsResult);
        if (expired != null)
        {
            return expired;
        }

        if (!enrolmentsResult.Success)
        {
            ViewData["ErrorMessage"] = enrolmentsResult.ErrorMessage;
        }

        var all = enrolmentsResult.Data ?? new List<EventEnrolmentDto>();

        var model = new EnrolmentsViewModel
        {
            Event = eventResult.Data,
            TotalCount = all.Count,
            SelectedCategoryId = categoryId,
            Categories = all
                .GroupBy(e => new { e.CategoryID, e.CategoryName })
                .Select(g => new CategoryCount
                {
                    CategoryID = g.Key.CategoryID,
                    CategoryName = g.Key.CategoryName,
                    Count = g.Count()
                })
                .OrderBy(c => c.CategoryName)
                .ToList(),
            Enrolments = all
                .Where(e => !categoryId.HasValue || e.CategoryID == categoryId.Value)
                .OrderBy(e => e.ParticipantName)
                .ThenBy(e => e.EnrolmentDate)
                .ToList()
        };

        return View(model);
    }
}
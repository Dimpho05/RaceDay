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
            PendingCount = all.Count(e => IsStatus(e, "Pending")),
            // Older enrolments created before statuses existed are called "Active"; they count as confirmed
            ConfirmedCount = all.Count(e => IsStatus(e, "Confirmed") || IsStatus(e, "Active")),
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

    // Confirm, cancel or reset one enrolment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int enrolmentId, int eventId, string status, int? categoryId)
    {
        if (status != "Pending" && status != "Confirmed" && status != "Cancelled")
        {
            TempData["Error"] = "That status is not valid.";
            return RedirectToAction(nameof(Index), new { eventId, categoryId });
        }

        var result = await _api.PutAsync($"api/enrolments/{enrolmentId}/status", new { status });

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (result.Success)
        {
            TempData["Success"] = status switch
            {
                "Confirmed" => "The enrolment was confirmed.",
                "Cancelled" => "The enrolment was cancelled.",
                _ => "The enrolment was set back to pending."
            };
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index), new { eventId, categoryId });
    }

    private static bool IsStatus(EventEnrolmentDto enrolment, string status)
        => string.Equals(enrolment.EnrolmentStatus, status, StringComparison.OrdinalIgnoreCase);
}
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class ManageCategoriesController : AppControllerBase
{
    private readonly IRaceDayApiClient _api;

    public ManageCategoriesController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // ---------- List + add form ----------

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var page = await BuildPageAsync(eventId, new CategoryFormViewModel { EventID = eventId });
        if (page.Redirect != null)
        {
            return page.Redirect;
        }

        return View(page.Model);
    }

    // ---------- Add ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel form)
    {
        ValidateAges(form);

        if (!ModelState.IsValid)
        {
            return await ShowIndexAgainAsync(form);
        }

        var result = await _api.PostAsync<CategoryDto>($"api/events/{form.EventID}/categories", BuildRequest(form));

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return await ShowIndexAgainAsync(form);
        }

        TempData["Success"] = "The category was added.";
        return RedirectToAction(nameof(Index), new { eventId = form.EventID });
    }

    // ---------- Edit ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var categoryResult = await _api.GetAsync<CategoryDto>($"api/categories/{id}");

        var expired = RedirectIfSessionExpired(categoryResult);
        if (expired != null)
        {
            return expired;
        }

        if (!categoryResult.Success || categoryResult.Data == null)
        {
            TempData["Error"] = categoryResult.IsNotFound
                ? "That category could not be found."
                : categoryResult.ErrorMessage;
            return RedirectToAction("Index", "ManageEvents");
        }

        var category = categoryResult.Data;

        // Loading the event also confirms it belongs to this Organiser
        var page = await BuildPageAsync(category.EventID, new CategoryFormViewModel());
        if (page.Redirect != null)
        {
            return page.Redirect;
        }

        return View(new CategoryFormViewModel
        {
            CategoryID = category.CategoryID,
            EventID = category.EventID,
            EventName = page.Model!.Event.EventName,
            CategoryName = category.CategoryName,
            MinimumAge = category.MinimumAge,
            MaximumAge = category.MaximumAge,
            CategoryDistance = category.CategoryDistance
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel form)
    {
        form.CategoryID = id;

        ValidateAges(form);

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _api.PutAsync($"api/categories/{id}", BuildRequest(form));

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(form);
        }

        TempData["Success"] = "The category was updated.";
        return RedirectToAction(nameof(Index), new { eventId = form.EventID });
    }

    // ---------- Delete ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await _api.DeleteAsync($"api/categories/{id}");

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (result.Success)
        {
            TempData["Success"] = "The category was deleted.";
        }
        else
        {
            // For example: participants have already enrolled in this category
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    // ---------- Helpers ----------

    // Loads the event and its categories; redirects away if the event is missing or not this Organiser's
    private async Task<(IActionResult? Redirect, CategoriesViewModel? Model)> BuildPageAsync(
        int eventId, CategoryFormViewModel form)
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
            TempData["Error"] = "You can only manage categories for your own events.";
            return (RedirectToAction("Index", "ManageEvents"), null);
        }

        var categoriesResult = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");

        expired = RedirectIfSessionExpired(categoriesResult);
        if (expired != null)
        {
            return (expired, null);
        }

        if (!categoriesResult.Success)
        {
            ViewData["ErrorMessage"] = categoriesResult.ErrorMessage;
        }

        var model = new CategoriesViewModel
        {
            Event = eventResult.Data,
            Categories = categoriesResult.Data ?? new List<CategoryDto>(),
            Form = form
        };

        return (null, model);
    }

    private async Task<IActionResult> ShowIndexAgainAsync(CategoryFormViewModel form)
    {
        var page = await BuildPageAsync(form.EventID, form);
        if (page.Redirect != null)
        {
            return page.Redirect;
        }

        return View("Index", page.Model);
    }

    private void ValidateAges(CategoryFormViewModel form)
    {
        if (form.MinimumAge.HasValue && form.MaximumAge.HasValue && form.MinimumAge > form.MaximumAge)
        {
            ModelState.AddModelError(nameof(form.MaximumAge),
                "The maximum age cannot be lower than the minimum age.");
        }
    }

    private static object BuildRequest(CategoryFormViewModel form) => new
    {
        categoryName = form.CategoryName,
        minimumAge = form.MinimumAge,
        maximumAge = form.MaximumAge,
        categoryDistance = form.CategoryDistance
    };
}
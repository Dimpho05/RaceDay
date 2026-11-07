using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class ManageEventsController : AppControllerBase
{
    private const long MaxBannerBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IRaceDayApiClient _api;

    public ManageEventsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // ---------- List ----------

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events");

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (!result.Success || result.Data == null)
        {
            ViewData["ErrorMessage"] = result.ErrorMessage;
            return View(new List<EventDto>());
        }

        // Only this Organiser's own events, newest date first
        var userId = HttpContext.Session.GetUserId();
        var mine = result.Data
            .Where(e => e.OrganiserID == userId)
            .OrderByDescending(e => e.EventDate)
            .ToList();

        return View(mine);
    }

    // ---------- Create ----------

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EventFormViewModel();

        var types = await FillEventTypesAsync(model);
        var expired = RedirectIfSessionExpired(types);
        if (expired != null)
        {
            return expired;
        }

        if (!types.Success)
        {
            TempData["Error"] = types.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        ValidateBanner(model);

        if (model.EventDate.HasValue && model.EventDate.Value <= DateTime.Now)
        {
            ModelState.AddModelError(nameof(model.EventDate), "The event date must be in the future.");
        }

        if (!ModelState.IsValid)
        {
            return await ShowFormAgainAsync("Create", model);
        }

        var result = await _api.PostAsync<EventDto>("api/events", BuildRequest(model));

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return await ShowFormAgainAsync("Create", model);
        }

        // The event now exists; send the banner (if any) through the API to Azure
        if (HasFile(model))
        {
            var upload = await _api.UploadAsync<EventDto>(
                $"api/events/{result.Data.EventID}/banner", model.BannerFile!);

            if (!upload.Success)
            {
                TempData["Error"] = "Your event was created, but the banner could not be uploaded: "
                    + upload.ErrorMessage;
                return RedirectToAction(nameof(Edit), new { id = result.Data.EventID });
            }
        }

        TempData["Success"] = "Your event was created. You can now add categories to it.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Edit ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
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
            return RedirectToAction(nameof(Index));
        }

        var ev = eventResult.Data;

        if (ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            TempData["Error"] = "You can only edit your own events.";
            return RedirectToAction(nameof(Index));
        }

        var model = new EventFormViewModel
        {
            EventID = ev.EventID,
            Name = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.Distance,
            EventTypeId = ev.EventTypeID,
            ExistingBannerUrl = ev.BannerUrl
        };

        var types = await FillEventTypesAsync(model);
        if (!types.Success)
        {
            TempData["Error"] = types.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventID = id;

        ValidateBanner(model);

        if (!ModelState.IsValid)
        {
            return await ShowFormAgainAsync("Edit", model);
        }

        var result = await _api.PutAsync<EventDto>($"api/events/{id}", BuildRequest(model));

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return await ShowFormAgainAsync("Edit", model);
        }

        if (HasFile(model))
        {
            var upload = await _api.UploadAsync<EventDto>($"api/events/{id}/banner", model.BannerFile!);

            if (!upload.Success)
            {
                TempData["Error"] = "Your changes were saved, but the banner could not be uploaded: "
                    + upload.ErrorMessage;
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        TempData["Success"] = "Your event was updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Delete ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteAsync($"api/events/{id}");

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (result.Success)
        {
            TempData["Success"] = "The event was deleted.";
        }
        else
        {
            // For example: "This event already has enrolments and cannot be deleted."
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------- Helpers ----------

    private async Task<ApiResult<List<EventTypeDto>>> FillEventTypesAsync(EventFormViewModel model)
    {
        var result = await _api.GetAsync<List<EventTypeDto>>("api/event-types");

        if (result.Success && result.Data != null)
        {
            model.EventTypes = result.Data
                .Select(t => new SelectListItem(t.TypeName, t.EventTypeID.ToString()))
                .ToList();
        }

        return result;
    }

    private async Task<IActionResult> ShowFormAgainAsync(string viewName, EventFormViewModel model)
    {
        await FillEventTypesAsync(model);
        return View(viewName, model);
    }

    private static object BuildRequest(EventFormViewModel model) => new
    {
        name = model.Name,
        description = model.Description,
        eventDate = model.EventDate,
        location = model.Location,
        distanceKm = model.DistanceKm,
        eventTypeId = model.EventTypeId
    };

    private static bool HasFile(EventFormViewModel model)
        => model.BannerFile != null && model.BannerFile.Length > 0;

    // Friendly checks before the file is sent on (the API checks again)
    private void ValidateBanner(EventFormViewModel model)
    {
        if (!HasFile(model))
        {
            return;
        }

        var file = model.BannerFile!;

        if (file.Length > MaxBannerBytes)
        {
            ModelState.AddModelError(nameof(model.BannerFile), "The image must be 5 MB or smaller.");
            return;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(model.BannerFile), "Only JPG, PNG or WEBP images are allowed.");
        }
    }
}
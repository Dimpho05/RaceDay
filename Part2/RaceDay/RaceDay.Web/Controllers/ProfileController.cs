using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Participant")]
public class ProfileController : AppControllerBase
{
    private const long MaxPictureBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IRaceDayApiClient _api;

    public ProfileController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // ---------- View ----------

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<UserDto>("api/profile");

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToUserHome();
        }

        return View(ToViewModel(result.Data));
    }

    // ---------- Update personal details ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDetails(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var result = await _api.PutAsync("api/profile", new
        {
            firstName = model.FirstName.Trim(),
            lastName = model.LastName.Trim(),
            phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
        });

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View("Index", model);
        }

        // Keep the name in the navigation bar up to date
        HttpContext.Session.SetString(SessionKeys.FirstName, model.FirstName.Trim());

        TempData["Success"] = "Your details were updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Upload profile picture ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image to upload.";
            return RedirectToAction(nameof(Index));
        }

        if (picture.Length > MaxPictureBytes)
        {
            TempData["Error"] = "The image must be 5 MB or smaller.";
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(picture.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            TempData["Error"] = "Only JPG, PNG or WEBP images are allowed.";
            return RedirectToAction(nameof(Index));
        }

        // The file goes to the API, which stores it in Azure Blob Storage
        var result = await _api.UploadAsync<UserDto>("api/profile/picture", picture);

        var expired = RedirectIfSessionExpired(result);
        if (expired != null)
        {
            return expired;
        }

        if (result.Success)
        {
            TempData["Success"] = "Your profile picture was updated.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(UserDto user) => new()
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        Email = user.Email,
        ProfilePictureUrl = user.ProfilePictureUrl
    };
}
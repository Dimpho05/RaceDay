using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Shared helpers for every controller in the app.
/// </summary>
public abstract class AppControllerBase : Controller
{
    // If the API says our login is no longer valid (for example the API was restarted),
    // end the MVC session too and send the user back to the login page.
    // Returns null when the session is fine.
    protected IActionResult? RedirectIfSessionExpired(ApiResult result)
    {
        if (!result.IsUnauthorized)
        {
            return null;
        }

        HttpContext.Session.Clear();
        TempData["Error"] = "Your session has expired. Please log in again.";
        return RedirectToAction("Login", "Account");
    }

    // Each role lands on its own home page after logging in
    protected IActionResult RedirectToUserHome()
    {
        return HttpContext.Session.GetRole() == "Organiser"
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Home");
    }
}
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly IRaceDayApiClient _api;

    public AccountController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.PostAsync<UserDto>("api/auth/login", new
        {
            email = model.Email,
            password = model.Password
        });

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(model);
        }

        HttpContext.Session.SignIn(result.Data);

        // Only follow return addresses on this site (stops redirects to other websites)
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var registerResult = await _api.PostAsync<UserDto>("api/auth/register", new
        {
            firstName = model.FirstName,
            lastName = model.LastName,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            phoneNumber = model.PhoneNumber
        });

        if (!registerResult.Success)
        {
            ModelState.AddModelError(string.Empty, registerResult.ErrorMessage);
            return View(model);
        }

        // Log the new user straight in so they do not have to type their details again
        var loginResult = await _api.PostAsync<UserDto>("api/auth/login", new
        {
            email = model.Email,
            password = model.Password
        });

        if (loginResult.Success && loginResult.Data != null)
        {
            HttpContext.Session.SignIn(loginResult.Data);
            return RedirectToAction("Index", "Home");
        }

        TempData["Success"] = "Your account was created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Tell the API to end its session too, then clear ours whatever the outcome
        if (HttpContext.Session.IsLoggedIn())
        {
            await _api.PostAsync("api/auth/logout");
        }

        HttpContext.Session.Clear();

        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
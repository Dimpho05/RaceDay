using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Organisers have their own home page: the dashboard
        if (HttpContext.Session.GetRole() == "Organiser")
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
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
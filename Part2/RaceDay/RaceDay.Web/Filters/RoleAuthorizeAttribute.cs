using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects a controller or action. Anonymous visitors are sent to the login page;
/// logged-in users with the wrong role are sent to an "access denied" page.
/// [RoleAuthorize] = any logged-in user, [RoleAuthorize("Organiser")] = Organisers only.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _requiredRole;

    public RoleAuthorizeAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsLoggedIn())
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Path + request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_requiredRole != null && session.GetRole() != _requiredRole)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
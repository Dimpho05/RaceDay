using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Services;

namespace RaceDay.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _requiredRole;

    // No role given = any logged-in user. A role given = only that role.
    public SessionAuthorizeAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetUserId();
        var role = session.GetRole();

        // Not logged in at all -> 401
        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult(
                new { message = "You must be logged in." });
            return;
        }

        // Logged in, but the wrong role -> 403
        if (_requiredRole != null && role != _requiredRole)
        {
            context.Result = new ObjectResult(
                new { message = $"This action requires the {_requiredRole} role." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
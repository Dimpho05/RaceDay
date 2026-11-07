using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public static class SessionExtensions
{
    public static int? GetUserId(this ISession session) => session.GetInt32(SessionKeys.UserId);

    // "Organiser" or "Participant", or null when nobody is logged in
    public static string? GetRole(this ISession session) => session.GetString(SessionKeys.Role);

    public static string? GetFirstName(this ISession session) => session.GetString(SessionKeys.FirstName);

    public static bool IsLoggedIn(this ISession session) => session.GetUserId() != null;

    // Remember who just logged in (their API cookie is stored separately by the API client)
    public static void SignIn(this ISession session, UserDto user)
    {
        session.SetInt32(SessionKeys.UserId, user.UserID);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.FirstName, user.FirstName);
    }
}
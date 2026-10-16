namespace RaceDay.Api.Services;

public static class SessionUserExtensions
{
    // The logged-in user's ID, or null if nobody is logged in
    public static int? GetUserId(this ISession session)
        => session.GetInt32(SessionKeys.UserId);

    // The logged-in user's role ("Organiser" or "Participant"), or null
    public static string? GetRole(this ISession session)
        => session.GetString(SessionKeys.Role);
}
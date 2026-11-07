namespace RaceDay.Web.Services;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FirstName = "FirstName";

    // The API's own session cookie (name=value), forwarded on every API call
    public const string ApiCookie = "ApiCookie";
}
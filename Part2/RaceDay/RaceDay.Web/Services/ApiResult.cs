namespace RaceDay.Web.Services;

/// <summary>
/// The outcome of one call to the RaceDay API. Controllers check Success and show
/// ErrorMessage (always user-friendly) instead of ever handling raw errors.
/// </summary>
public class ApiResult
{
    public bool Success { get; init; }

    // 0 means the API could not be reached at all
    public int StatusCode { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public bool IsUnauthorized => StatusCode == 401;
    public bool IsForbidden => StatusCode == 403;
    public bool IsNotFound => StatusCode == 404;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}
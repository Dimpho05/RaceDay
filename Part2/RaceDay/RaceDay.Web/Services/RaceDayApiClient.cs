using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class RaceDayApiClient : IRaceDayApiClient
{
    private const string ApiSessionCookieName = ".AspNetCore.Session";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RaceDayApiClient> _logger;

    public RaceDayApiClient(
        HttpClient http,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<ApiResult<T>> GetAsync<T>(string path)
        => SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path));

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null)
        => SendAsync<T>(JsonRequest(HttpMethod.Post, path, body));

    public async Task<ApiResult> PostAsync(string path, object? body = null)
        => await SendAsync<object>(JsonRequest(HttpMethod.Post, path, body));

    public Task<ApiResult<T>> PutAsync<T>(string path, object body)
        => SendAsync<T>(JsonRequest(HttpMethod.Put, path, body));

    public async Task<ApiResult> PutAsync(string path, object body)
        => await SendAsync<object>(JsonRequest(HttpMethod.Put, path, body));

    public async Task<ApiResult> DeleteAsync(string path)
        => await SendAsync<object>(new HttpRequestMessage(HttpMethod.Delete, path));

    public Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        form.Add(fileContent, "file", file.FileName);

        return SendAsync<T>(new HttpRequestMessage(HttpMethod.Post, path) { Content = form });
    }

    private static HttpRequestMessage JsonRequest(HttpMethod method, string path, object? body)
    {
        var request = new HttpRequestMessage(method, path);
        if (body != null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request)
    {
        var session = _httpContextAccessor.HttpContext?.Session;

        // Forward this user's API session cookie so the API knows who is calling
        var cookie = session?.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        try
        {
            using (request)
            using (var response = await _http.SendAsync(request))
            {
                CaptureApiSessionCookie(response, session);

                var content = await response.Content.ReadAsStringAsync();
                var status = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    return new ApiResult<T>
                    {
                        Success = false,
                        StatusCode = status,
                        ErrorMessage = BuildErrorMessage(status, content)
                    };
                }

                return new ApiResult<T>
                {
                    Success = true,
                    StatusCode = status,
                    Data = TryDeserialize<T>(content)
                };
            }
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            _logger.LogError(ex, "Could not reach the RaceDay API.");
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = 0,
                ErrorMessage = BuildErrorMessage(0, string.Empty)
            };
        }
    }

    // When the API starts a session (on login), remember its cookie for later calls
    private static void CaptureApiSessionCookie(HttpResponseMessage response, ISession? session)
    {
        if (session == null || !response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return;
        }

        foreach (var value in values)
        {
            if (value.StartsWith(ApiSessionCookieName + "=", StringComparison.OrdinalIgnoreCase))
            {
                // Keep only "name=value", dropping path, httponly, etc.
                session.SetString(SessionKeys.ApiCookie, value.Split(';')[0]);
            }
        }
    }

    private static T? TryDeserialize<T>(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    // Turns whatever the API sent back into a short message that is safe to show a user
    private static string BuildErrorMessage(int status, string content)
    {
        if (status == 0)
        {
            return "We could not reach the RaceDay service. Please try again in a moment.";
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(message.GetString()))
                {
                    return message.GetString()!;
                }

                // Validation errors look like { "errors": { "Email": ["..."] } }
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(p => p.Value.EnumerateArray())
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString())
                        .Where(s => !string.IsNullOrWhiteSpace(s));

                    var joined = string.Join(" ", messages);
                    if (joined.Length > 0)
                    {
                        return joined;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON; fall through to the generic messages below
        }

        return status switch
        {
            401 => "Please log in to continue.",
            403 => "You do not have permission to do that.",
            404 => "We could not find what you were looking for.",
            >= 500 => "Something went wrong on our side. Please try again.",
            _ => "That request could not be completed. Please check your input and try again."
        };
    }
}
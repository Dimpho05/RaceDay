namespace RaceDay.Web.Services;

/// <summary>
/// Talks to the RaceDay API on behalf of the current user.
/// The MVC app has no database access and no business logic; everything goes through here.
/// </summary>
public interface IRaceDayApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string path);

    Task<ApiResult<T>> PostAsync<T>(string path, object? body = null);
    Task<ApiResult> PostAsync(string path, object? body = null);

    Task<ApiResult<T>> PutAsync<T>(string path, object body);
    Task<ApiResult> PutAsync(string path, object body);

    Task<ApiResult> DeleteAsync(string path);

    /// <summary>Sends an uploaded image to the API as multipart/form-data (field name "file").</summary>
    Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file);
}
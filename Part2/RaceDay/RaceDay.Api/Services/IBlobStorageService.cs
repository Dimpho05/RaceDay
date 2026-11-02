namespace RaceDay.Api.Services;

/// <summary>
/// Handles storing and removing image files in Azure Blob Storage.
/// Only the API talks to Azure; the MVC app always goes through the API.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file and returns its public URL.
    /// </summary>
    /// <param name="content">The file contents.</param>
    /// <param name="fileName">Original file name (only the extension is kept).</param>
    /// <param name="contentType">For example image/png.</param>
    /// <param name="folder">Virtual folder inside the container, e.g. "banners" or "profiles".</param>
    Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder);

    /// <summary>
    /// Deletes a previously uploaded file using the URL that UploadAsync returned.
    /// Does nothing if the URL is empty or does not belong to this container.
    /// </summary>
    Task DeleteAsync(string? blobUrl);
}
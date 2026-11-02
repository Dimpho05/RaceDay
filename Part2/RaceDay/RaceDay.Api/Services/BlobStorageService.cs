using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage:ConnectionString is not configured.");
        var containerName = configuration["BlobStorage:ContainerName"] ?? "raceday-images";

        // Creating the client does not connect to Azure yet, so the API can start
        // even when the storage account (or Azurite) is not running.
        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder)
    {
        // Public read access on blobs so the browser can show the images directly
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";

        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string? blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl) || !Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            return;
        }

        // The blob name is everything after "/<container>/" in the URL path
        var marker = $"/{_container.Name}/";
        var index = uri.AbsolutePath.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return;
        }

        var blobName = Uri.UnescapeDataString(uri.AbsolutePath[(index + marker.Length)..]);
        await _container.DeleteBlobIfExistsAsync(blobName);
    }
}
using RaceDay.Api.Services;

namespace RaceDay.Tests;

// Stands in for Azure during tests so they run anywhere with no storage account.
public class FakeBlobStorageService : IBlobStorageService
{
    public List<string> DeletedUrls { get; } = new();

    public Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return Task.FromResult($"https://fake-storage.test/raceday-images/{folder}/{Guid.NewGuid():N}{extension}");
    }

    public Task DeleteAsync(string? blobUrl)
    {
        if (!string.IsNullOrWhiteSpace(blobUrl))
        {
            DeletedUrls.Add(blobUrl);
        }

        return Task.CompletedTask;
    }
}
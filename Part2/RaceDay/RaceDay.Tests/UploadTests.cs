using System.Net;
using System.Net.Http.Headers;

namespace RaceDay.Tests;

public class UploadTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UploadTests(ApiFactory factory) => _factory = factory;

    // The first bytes of a real PNG file, plus some padding
    private static readonly byte[] PngBytes =
        { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 };

    private static MultipartFormDataContent FileContent(byte[] bytes, string fileName, string contentType)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Organiser_CanUploadBannerForOwnEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner",
            FileContent(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("raceday-images/banners/", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Organiser_CannotUploadBannerForSomeoneElsesEvent()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);
        var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await otherOrganiser.PostAsync($"/api/events/{eventId}/banner",
            FileContent(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUploadBanner()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsync($"/api/events/{eventId}/banner",
            FileContent(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithNonImageFile_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var textBytes = System.Text.Encoding.UTF8.GetBytes("this is not an image at all");

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner",
            FileContent(textBytes, "notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithRenamedNonImage_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var textBytes = System.Text.Encoding.UTF8.GetBytes("this is text pretending to be a png");

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner",
            FileContent(textBytes, "fake.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithNoFile_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner",
            new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ForMissingEvent_ReturnsNotFound()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsync("/api/events/99999/banner",
            FileContent(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var anonymous = TestHelpers.NewClient(_factory);

        var response = await anonymous.PostAsync("/api/events/1/banner",
            FileContent(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoggedInUser_CanUploadProfilePicture()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsync("/api/profile/picture",
            FileContent(PngBytes, "me.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("raceday-images/profiles/", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UploadProfilePicture_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var anonymous = TestHelpers.NewClient(_factory);

        var response = await anonymous.PostAsync("/api/profile/picture",
            FileContent(PngBytes, "me.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
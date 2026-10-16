using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ResultTests(ApiFactory factory) => _factory = factory;

    // Sets up an event with one enrolled participant and returns everything the tests need
    private async Task<(HttpClient Organiser, HttpClient Participant, int EventId, int EnrolmentId)> SetUpAsync()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await TestHelpers.EnrolAsync(participant, eventId, categoryId);
        return (organiser, participant, eventId, enrolmentId);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var (organiser, participant, _, enrolmentId) = await SetUpAsync();

        var create = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "01:38:42", finishingPosition = 3 });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var mine = await participant.GetAsync("/api/results/my");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var body = await mine.Content.ReadAsStringAsync();
        Assert.Contains("01:38:42", body);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var (organiser, _, _, enrolmentId) = await SetUpAsync();
        await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "00:45:00", finishingPosition = 1 });

        var second = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "00:46:00", finishingPosition = 2 });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithBadTimeFormat_ReturnsBadRequest()
    {
        var (organiser, _, _, enrolmentId) = await SetUpAsync();

        var response = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "fast", finishingPosition = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForUnknownEnrolment_ReturnsNotFound()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId = 99999, finishTime = "01:00:00", finishingPosition = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent()
    {
        var (_, _, _, enrolmentId) = await SetUpAsync();
        var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await otherOrganiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "01:00:00", finishingPosition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnResult()
    {
        var (organiser, participant, _, enrolmentId) = await SetUpAsync();
        var create = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "01:10:00", finishingPosition = 5 });
        using var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var resultId = doc.RootElement.GetProperty("resultID").GetInt32();

        var update = await organiser.PutAsJsonAsync($"/api/results/{resultId}",
            new { finishTime = "01:05:30", finishingPosition = 2 });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Contains("01:05:30", await participant.GetStringAsync("/api/results/my"));
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersResult()
    {
        var (organiser, _, _, enrolmentId) = await SetUpAsync();
        var create = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "01:10:00", finishingPosition = 5 });
        using var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var resultId = doc.RootElement.GetProperty("resultID").GetInt32();
        var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await otherOrganiser.PutAsJsonAsync($"/api/results/{resultId}",
            new { finishTime = "00:30:00", finishingPosition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_ThatDoesNotExist_ReturnsNotFound()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/results/99999",
            new { finishTime = "00:30:00", finishingPosition = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var (organiser, participant, _, enrolmentId) = await SetUpAsync();
        await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "00:59:59", finishingPosition = 1 });
        var stranger = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var strangerResults = await stranger.GetStringAsync("/api/results/my");

        Assert.DoesNotContain("00:59:59", strangerResults);
        Assert.Contains("00:59:59", await participant.GetStringAsync("/api/results/my"));
    }

    [Fact]
    public async Task EventResults_AreVisibleToOwnerAndEnrolledParticipant_ButNotToOthers()
    {
        var (organiser, participant, eventId, enrolmentId) = await SetUpAsync();
        await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "00:52:10", finishingPosition = 1 });
        var stranger = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync($"/api/events/{eventId}/results")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync($"/api/events/{eventId}/results")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/events/{eventId}/results")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOrganiser.GetAsync($"/api/events/{eventId}/results")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.GetAsync("/api/events/99999/results")).StatusCode);
    }
}
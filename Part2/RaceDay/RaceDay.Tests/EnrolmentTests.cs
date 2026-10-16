using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EnrolmentTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser, "Enrol Here");
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId, "Senior");

        var response = await participant.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Enrol Here", body);
        Assert.Contains("Senior", body);

        // The participant sees it in their own list
        var mine = await participant.GetStringAsync("/api/enrolments/my");
        Assert.Contains("Enrol Here", mine);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        await TestHelpers.EnrolAsync(participant, eventId, categoryId);

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Participant", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsForAnotherOrganisersEvent()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);

        var response = await other.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_ReturnsConflict()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        await TestHelpers.EnrolAsync(participant, eventId, categoryId);

        var second = await participant.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventA = await TestHelpers.CreateEventAsync(organiser, "Event A");
        var eventB = await TestHelpers.CreateEventAsync(organiser, "Event B");
        var categoryOfB = await TestHelpers.CreateCategoryAsync(organiser, eventB);

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new { eventId = eventA, categoryId = categoryOfB });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InEventThatDoesNotExist_ReturnsNotFound()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/enrolments", new { eventId = 99999, categoryId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await TestHelpers.EnrolAsync(participant, eventId, categoryId);

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await TestHelpers.EnrolAsync(owner, eventId, categoryId);

        var response = await other.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
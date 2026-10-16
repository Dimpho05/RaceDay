using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

// Checks that each role is kept out of the other role's functionality
public class RoleEnforcementTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RoleEnforcementTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.EventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var id = await TestHelpers.CreateEventAsync(organiser);

        var put = await participant.PutAsJsonAsync($"/api/events/{id}", TestHelpers.EventBody("Nope"));
        var delete = await participant.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Sneaky",
            minimumAge = 18,
            maximumAge = 40,
            categoryDistance = 10
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResults()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = 1,
            finishTime = "01:00:00",
            finishingPosition = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);

        var response = await organiser.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewMyEnrolmentsOrMyResults()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/enrolments/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/results/my")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_IsRejectedFromEveryProtectedArea()
    {
        var anonymous = TestHelpers.NewClient(_factory);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/events/1/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/enrolments/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/results/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/enrolments", new { eventId = 1, categoryId = 1 })).StatusCode);
    }
}
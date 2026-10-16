using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.EventBody("Sunrise 10km"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Sunrise 10km", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithMissingName_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            description = "No name",
            eventDate = DateTime.UtcNow.AddDays(10),
            location = "Pretoria",
            distanceKm = 5,
            eventTypeId = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithUnknownEventType_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Bad type",
            eventDate = DateTime.UtcNow.AddDays(10),
            location = "Pretoria",
            distanceKm = 5,
            eventTypeId = 99
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(organiser, "Before");

        var response = await organiser.PutAsJsonAsync($"/api/events/{id}", TestHelpers.EventBody("After"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("After", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{id}", TestHelpers.EventBody("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateMissingEvent_ReturnsNotFound()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/events/99999", TestHelpers.EventBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(owner);

        var response = await other.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteEventThatHasEnrolments()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        await TestHelpers.EnrolAsync(participant, eventId, categoryId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task BothRoles_CanViewEvents()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var id = await TestHelpers.CreateEventAsync(organiser, "Visible To All");

        var list = await participant.GetAsync("/api/events");
        var single = await organiser.GetAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("Visible To All", await list.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ThatDoesNotExist_ReturnsNotFound()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.GetAsync("/api/events/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Events_WithoutLogin_ReturnsUnauthorized()
    {
        var anonymous = TestHelpers.NewClient(_factory);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/events", TestHelpers.EventBody())).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateAndViewCategories()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        await TestHelpers.CreateCategoryAsync(organiser, eventId, "Under 20");

        var response = await participant.GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Under 20", await response.Content.ReadAsStringAsync());
    }
}
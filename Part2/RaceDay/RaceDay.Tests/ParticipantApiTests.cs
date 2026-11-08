using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

// Tests for the API features the Participant pages rely on:
// event filters, enrolment statuses and finisher counts.
public class ParticipantApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ParticipantApiTests(ApiFactory factory) => _factory = factory;

    // ----- Event filters -----

    [Fact]
    public async Task Events_CanBeFilteredBySearchText()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var unique = "Zebra" + Guid.NewGuid().ToString("N")[..8];
        await TestHelpers.CreateEventAsync(organiser, unique);
        await TestHelpers.CreateEventAsync(organiser, "Something Else " + Guid.NewGuid().ToString("N")[..8]);

        var response = await organiser.GetAsync($"/api/events?search={unique}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(unique, body);
        Assert.DoesNotContain("Something Else", body);
    }

    [Fact]
    public async Task Events_CanBeFilteredByType()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        await TestHelpers.CreateEventAsync(organiser, "Type Filter " + Guid.NewGuid().ToString("N")[..8]);

        // The helper creates Run events (type 1), so asking for Cycle (type 3) must not return them
        var response = await organiser.GetAsync("/api/events?eventTypeId=3&search=Type Filter");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Type Filter", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Events_UpcomingOnly_HidesEventsFromThePast()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var name = "PastRace" + Guid.NewGuid().ToString("N")[..8];

        var create = await organiser.PostAsJsonAsync("/api/events", new
        {
            name,
            description = "Already happened",
            eventDate = DateTime.Now.AddDays(1),
            location = "Pretoria",
            distanceKm = 5,
            eventTypeId = 1
        });
        create.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var eventId = doc.RootElement.GetProperty("eventID").GetInt32();

        // Move the event into the past
        var update = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            name,
            description = "Already happened",
            eventDate = DateTime.Now.AddDays(-10),
            location = "Pretoria",
            distanceKm = 5,
            eventTypeId = 1
        });
        update.EnsureSuccessStatusCode();

        var upcoming = await organiser.GetAsync($"/api/events?search={name}&upcomingOnly=true");
        var all = await organiser.GetAsync($"/api/events?search={name}");

        Assert.DoesNotContain(name, await upcoming.Content.ReadAsStringAsync());
        Assert.Contains(name, await all.Content.ReadAsStringAsync());
    }

    // ----- Enrolment statuses -----

    [Fact]
    public async Task NewEnrolment_StartsAsPending()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);

        var response = await participant.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Pending", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Organiser_CanConfirmAnEnrolment()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await TestHelpers.EnrolAsync(participant, eventId, categoryId);

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/status",
            new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Confirmed", await response.Content.ReadAsStringAsync());

        var mine = await participant.GetAsync("/api/enrolments/my");
        Assert.Contains("Confirmed", await mine.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidValue_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await TestHelpers.EnrolAsync(participant, eventId, categoryId);

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/status",
            new { status = "Maybe" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_ByAnotherOrganiser_ReturnsForbidden()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(owner);
        var categoryId = await TestHelpers.CreateCategoryAsync(owner, eventId);
        var enrolmentId = await TestHelpers.EnrolAsync(participant, eventId, categoryId);

        var response = await otherOrganiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/status",
            new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_AsParticipant_ReturnsForbidden()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await TestHelpers.EnrolAsync(participant, eventId, categoryId);

        var response = await participant.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/status",
            new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ----- Finisher counts -----

    [Fact]
    public async Task MyResults_IncludeTheNumberOfFinishers()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var first = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var second = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var firstEnrolment = await TestHelpers.EnrolAsync(first, eventId, categoryId);
        var secondEnrolment = await TestHelpers.EnrolAsync(second, eventId, categoryId);

        (await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId = firstEnrolment, finishTime = "00:40:00", finishingPosition = 1 }))
            .EnsureSuccessStatusCode();
        (await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId = secondEnrolment, finishTime = "00:45:00", finishingPosition = 2 }))
            .EnsureSuccessStatusCode();

        var response = await second.GetAsync("/api/results/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var result = doc.RootElement.EnumerateArray().First();
        Assert.Equal(2, result.GetProperty("finishingPosition").GetInt32());
        Assert.Equal(2, result.GetProperty("totalFinishers").GetInt32());
    }
}
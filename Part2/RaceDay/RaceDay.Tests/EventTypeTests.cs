using System.Net;

namespace RaceDay.Tests;

public class EventTypeTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventTypeTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task LoggedInUser_CanListEventTypes()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.GetAsync("/api/event-types");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Run", body);
        Assert.Contains("Walk", body);
        Assert.Contains("Cycle", body);
    }

    [Fact]
    public async Task ListEventTypes_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var anonymous = TestHelpers.NewClient(_factory);

        var response = await anonymous.GetAsync("/api/event-types");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
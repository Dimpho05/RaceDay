using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public static class TestHelpers
{
    public const string Password = "Passw0rd!123";

    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    // A fresh client has its own cookie jar, so it behaves like one browser / one user
    public static HttpClient NewClient(ApiFactory factory)
        => factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    public static async Task<string> RegisterAsync(HttpClient client, string role)
    {
        var email = $"{role.ToLower()}-{Guid.NewGuid():N}@test.com";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = role,
            email,
            password = Password,
            role,
            phoneNumber = "0820000000"
        });
        response.EnsureSuccessStatusCode();
        return email;
    }

    public static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password)
        => await client.PostAsJsonAsync("/api/auth/login", new { email, password });

    // Registers a new user of the given role and logs them in on a brand new client
    public static async Task<HttpClient> LoggedInClientAsync(ApiFactory factory, string role)
    {
        var client = NewClient(factory);
        var email = await RegisterAsync(client, role);
        (await LoginAsync(client, email)).EnsureSuccessStatusCode();
        return client;
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<T>(Json))!;

    public static object EventBody(string name = "Test Run") => new
    {
        name,
        description = "A test event",
        eventDate = DateTime.UtcNow.AddDays(30),
        location = "Pretoria",
        distanceKm = 10,
        eventTypeId = 1
    };

    // Creates an event as the given Organiser and returns its ID
    public static async Task<int> CreateEventAsync(HttpClient organiser, string name = "Test Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody(name));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("eventID").GetInt32();
    }

    // Creates a category for an event and returns its ID
    public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, string name = "Senior")
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = name,
            minimumAge = 18,
            maximumAge = 99,
            categoryDistance = 10
        });
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("categoryID").GetInt32();
    }

    // Enrols a Participant and returns the enrolment ID
    public static async Task<int> EnrolAsync(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("enrolmentID").GetInt32();
    }
}
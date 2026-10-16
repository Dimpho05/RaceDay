using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_WithValidDetails_ReturnsCreated_AndNoPassword(string role)
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Thandi",
            lastName = "Dlamini",
            email = $"{Guid.NewGuid():N}@test.com",
            password = TestHelpers.Password,
            role
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(role, body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = await TestHelpers.RegisterAsync(client, "Participant");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Copy",
            lastName = "Cat",
            email,
            password = TestHelpers.Password,
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Bad",
            lastName = "Role",
            email = $"{Guid.NewGuid():N}@test.com",
            password = TestHelpers.Password,
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Short",
            lastName = "Pass",
            email = $"{Guid.NewGuid():N}@test.com",
            password = "123",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOk_AndUserRole()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = await TestHelpers.RegisterAsync(client, "Organiser");

        var response = await TestHelpers.LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Organiser", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = await TestHelpers.RegisterAsync(client, "Participant");

        var response = await TestHelpers.LoginAsync(client, email, "WrongPassword!1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await TestHelpers.LoginAsync(client, "nobody@test.com");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutLogin_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_AfterLogin_ReturnsOwnDetails()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = await TestHelpers.RegisterAsync(client, "Participant");
        await TestHelpers.LoginAsync(client, email);

        var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(email, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Profile_Update_ChangesDetails()
    {
        var client = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await client.PutAsJsonAsync("/api/profile", new
        {
            firstName = "Updated",
            lastName = "Name",
            phoneNumber = "0831112222"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await client.GetStringAsync("/api/profile");
        Assert.Contains("Updated", profile);
        Assert.Contains("0831112222", profile);
    }

    [Fact]
    public async Task Logout_EndsSession_SoProtectedCallsAreRejected()
    {
        var client = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/profile")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutLogin_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
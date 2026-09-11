using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Users.Commands.RegisterUser;
using TimeTracker.Domain.Enums;
using Xunit;

namespace TimeTracker.API.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> LoginAsSeededEmployerAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "employer@demo.local", Password = "Password123!" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        return body!.Token;
    }

    [Fact]
    public async Task EmployerCreatesUser_ThenNewUserLogsIn_ReturnsToken()
    {
        var employerToken = await LoginAsSeededEmployerAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employerToken);

        var email = $"{Guid.NewGuid()}@test.local";
        request.Content = JsonContent.Create(new RegisterUserCommand("Test", "User", email, "Password123!", UserRole.Employee));

        var createResponse = await _client.SendAsync(request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = "Password123!" });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await loginResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateUser_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/users",
            new RegisterUserCommand("Test", "User", $"{Guid.NewGuid()}@test.local", "Password123!", UserRole.Employee));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_AsNonEmployer_ReturnsForbidden()
    {
        var employerToken = await LoginAsSeededEmployerAsync();

        // First create a plain Employee, then use their token to try (and fail) to create another user.
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employerToken);
        var email = $"{Guid.NewGuid()}@test.local";
        createRequest.Content = JsonContent.Create(new RegisterUserCommand("Test", "Employee", email, "Password123!", UserRole.Employee));
        await _client.SendAsync(createRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Password123!" });
        var employeeToken = (await loginResponse.Content.ReadFromJsonAsync<AuthResultDto>())!.Token;

        using var forbiddenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        forbiddenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        forbiddenRequest.Content = JsonContent.Create(
            new RegisterUserCommand("Nope", "Blocked", $"{Guid.NewGuid()}@test.local", "Password123!", UserRole.Employee));

        var response = await _client.SendAsync(forbiddenRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nobody@test.local", Password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TimeEntries_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/timeentries");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

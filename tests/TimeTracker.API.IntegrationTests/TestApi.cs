using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Tenants.Commands.CreateTenant;
using TimeTracker.Application.Tenants.Dtos;

namespace TimeTracker.API.IntegrationTests;

/// <summary>Small helpers for driving the real API from integration tests.</summary>
public static class TestApi {
    public const string Password = "Password123!";

    public record CreatedTenant(TenantDto Tenant, string EmployerEmail);

    public static async Task<string> LoginAsync(HttpClient client, string email, string password) {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!.Token;
    }

    public static Task<string> LoginAsAdminAsync(HttpClient client) =>
        LoginAsync(client, CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

    /// <summary>Creates a fresh tenant (as the seeded platform admin) with a unique employer account.</summary>
    public static async Task<CreatedTenant> CreateTenantAsync(HttpClient client) {
        var adminToken = await LoginAsAdminAsync(client);
        var employerEmail = $"{Guid.NewGuid()}@employer.local";

        using var request = Authorized(HttpMethod.Post, "/api/tenants", adminToken);
        request.Content = JsonContent.Create(
            new CreateTenantCommand($"Tenant {Guid.NewGuid()}", "Test", "Employer", employerEmail, Password));

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenant = (await response.Content.ReadFromJsonAsync<TenantDto>())!;

        return new CreatedTenant(tenant, employerEmail);
    }

    public static HttpRequestMessage Authorized(HttpMethod method, string url, string token) {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}

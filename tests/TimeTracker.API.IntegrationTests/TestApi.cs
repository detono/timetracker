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

    /// <summary>
    /// Signs in. New accounts must replace their first password before anything else; this
    /// does that (and changes it back), so tests can keep using <paramref name="password"/>.
    /// </summary>
    public static async Task<string> LoginAsync(HttpClient client, string email, string password) {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
        if (!result.MustChangePassword) {
            return result.Token;
        }

        const string interim = "Interim-Password-1";
        var token = await ChangePasswordAsync(client, result.Token, null, interim);
        return await ChangePasswordAsync(client, token, interim, password);
    }

    private static async Task<string> ChangePasswordAsync(HttpClient client, string token, string? current, string next) {
        using var request = Authorized(HttpMethod.Post, "/api/auth/change-password", token);
        request.Content = JsonContent.Create(new { CurrentPassword = current, NewPassword = next });
        var response = await client.SendAsync(request);
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

    /// <summary>Sends a request as the given user, with an optional JSON body.</summary>
    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string url, string token, object? body = null
    ) {
        using var request = Authorized(method, url, token);
        if (body is not null) {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    /// <summary>Like <see cref="SendAsync"/>, but expects a 2xx answer and reads its JSON.</summary>
    public static async Task<T> SendOkAsync<T>(
        HttpClient client, HttpMethod method, string url, string token, object? body = null
    ) {
        var response = await SendAsync(client, method, url, token, body);
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    /// <summary>Creates an employee in the employer's company and signs them in.</summary>
    public static async Task<(Guid Id, string Email, string Token)> CreateEmployeeAsync(
        HttpClient client, string employerToken, string firstName = "Eva", string lastName = "Employee"
    ) {
        var email = $"{Guid.NewGuid()}@employee.local";
        var user = await SendOkAsync<UserIdOnly>(client, HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = firstName, LastName = lastName, Email = email, Password, Role = "Employee"
        });
        return (user.Id, email, await LoginAsync(client, email, Password));
    }

    public record UserIdOnly(Guid Id);
}

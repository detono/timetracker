using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.Projects.Dtos;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Application.Users.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>
/// End-to-end proof that two tenants on the same installation can't see or touch each
/// other's data, and that platform admins are kept out of tenant data entirely.
/// </summary>
public class TenantIsolationTests : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client;

    public TenantIsolationTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private async Task<T> GetAsync<T>(string url, string token) {
        using var request = Authorized(HttpMethod.Get, url, token);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) {
        using var request = Authorized(method, url, token);
        if (body is not null) {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task TenantsCannotSeeOrChangeEachOthersData() {
        var tenantA = await CreateTenantAsync(_client);
        var tenantB = await CreateTenantAsync(_client);
        var tokenA = await LoginAsync(_client, tenantA.EmployerEmail, Password);
        var tokenB = await LoginAsync(_client, tenantB.EmployerEmail, Password);

        // Tenant A creates a project and logs time on it with one of its own hour types.
        var projectResponse = await SendAsync(HttpMethod.Post, "/api/projects", tokenA, new { Name = "Secret A", ClientName = "A" });
        projectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projectAId = await projectResponse.Content.ReadFromJsonAsync<Guid>();

        var hourTypesA = await GetAsync<List<HourTypeDto>>("/api/hourtypes", tokenA);
        var hourTypesB = await GetAsync<List<HourTypeDto>>("/api/hourtypes", tokenB);
        hourTypesA.Should().HaveCount(3);
        hourTypesB.Should().HaveCount(3);
        hourTypesA.Select(h => h.Id).Should().NotIntersectWith(hourTypesB.Select(h => h.Id));

        var entryResponse = await SendAsync(HttpMethod.Post, "/api/timeentries", tokenA, new {
            HourTypeId = hourTypesA[0].Id, ProjectId = projectAId, WorkDate = "2026-10-26",
            StartTime = "09:00", EndTime = "17:00", BreakMinutes = 30, Notes = "A's work"
        });
        entryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var entryA = (await entryResponse.Content.ReadFromJsonAsync<TimeEntryDto>())!;

        // Tenant B sees none of it.
        (await GetAsync<List<ProjectDto>>("/api/projects", tokenB)).Should().NotContain(p => p.Id == projectAId);
        (await GetAsync<List<UserDto>>("/api/users", tokenB)).Should().ContainSingle()
            .Which.Email.Should().Be(tenantB.EmployerEmail);
        (await GetAsync<List<TimeEntryDto>>("/api/timeentries/team?from=2026-10-01&to=2026-10-31", tokenB))
            .Should().BeEmpty();

        // ...and can't change, delete, or reference it.
        (await SendAsync(HttpMethod.Delete, $"/api/timeentries/{entryA.Id}", tokenB)).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(HttpMethod.Post, $"/api/users/{entryA.UserId}/activate", tokenB)).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(HttpMethod.Post, "/api/timeentries", tokenB, new {
            HourTypeId = hourTypesB[0].Id, ProjectId = projectAId, WorkDate = "2026-10-26",
            StartTime = "09:00", EndTime = "10:00", BreakMinutes = 0
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SendAsync(HttpMethod.Post, "/api/timeentries", tokenB, new {
            HourTypeId = hourTypesA[0].Id, WorkDate = "2026-10-26",
            StartTime = "09:00", EndTime = "10:00", BreakMinutes = 0
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Tenant A's entry is untouched.
        (await GetAsync<List<TimeEntryDto>>("/api/timeentries?from=2026-10-01&to=2026-10-31", tokenA))
            .Should().ContainSingle(e => e.Id == entryA.Id);
    }

    [Fact]
    public async Task PlatformAdmin_CannotReachTenantData() {
        var adminToken = await LoginAsAdminAsync(_client);

        (await SendAsync(HttpMethod.Get, "/api/users", adminToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Get, "/api/timeentries", adminToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Get, "/api/projects", adminToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Employer_CannotReachPlatformAdminEndpoints() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);

        (await SendAsync(HttpMethod.Get, "/api/tenants", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Get, "/api/platform-admins", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Employer_CannotCreatePlatformAdminThroughUsersEndpoint() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);

        var response = await SendAsync(HttpMethod.Post, "/api/users", token, new {
            FirstName = "Sneaky", LastName = "Admin", Email = $"{Guid.NewGuid()}@test.local",
            Password = Password, Role = "PlatformAdmin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeactivatedTenant_CannotLogIn() {
        var tenant = await CreateTenantAsync(_client);
        var adminToken = await LoginAsAdminAsync(_client);

        (await SendAsync(HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/deactivate", adminToken)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { Email = tenant.EmployerEmail, Password });
        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EmailIsUniqueAcrossTenants() {
        var tenantA = await CreateTenantAsync(_client);
        var tenantB = await CreateTenantAsync(_client);
        var tokenB = await LoginAsync(_client, tenantB.EmployerEmail, Password);

        var response = await SendAsync(HttpMethod.Post, "/api/users", tokenB, new {
            FirstName = "Copy", LastName = "Cat", Email = tenantA.EmployerEmail, Password, Role = "Employee"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PlatformAdmin_CanCreateAnotherAdminWhoCanLogIn() {
        var adminToken = await LoginAsAdminAsync(_client);
        var email = $"{Guid.NewGuid()}@platform.local";

        var response = await SendAsync(HttpMethod.Post, "/api/platform-admins", adminToken,
            new { FirstName = "Second", LastName = "Admin", Email = email, Password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newAdminToken = await LoginAsync(_client, email, Password);
        (await GetAsync<List<UserDto>>("/api/platform-admins", newAdminToken))
            .Should().Contain(a => a.Email == email).And.OnlyContain(a => a.Role == "PlatformAdmin");
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TimeTracker.Application.AuditLog;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.Tenants.Dtos;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Application.Users.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

public class AuditLogTests : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client;

    public AuditLogTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) {
        using var request = Authorized(method, url, token);
        if (body is not null) {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private async Task<T> SendOkAsync<T>(HttpMethod method, string url, string token, object? body = null) {
        var response = await SendAsync(method, url, token, body);
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<HttpStatusCode> StatusAsync(HttpMethod method, string url, string token, object? body = null) =>
        (await SendAsync(method, url, token, body)).StatusCode;

    [Fact]
    public async Task EmployersSeeWhoChangedWhat_NewestFirst_OnlyForTheirOwnCompany() {
        var tenant = await CreateTenantAsync(_client);
        var other = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var otherToken = await LoginAsync(_client, other.EmployerEmail, Password);
        var email = $"{Guid.NewGuid()}@employee.local";
        var employee = await SendOkAsync<UserDto>(HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = "Eva", LastName = "Employee", Email = email, Password, Role = "Employee"
        });
        var employeeToken = await LoginAsync(_client, email, Password);
        var work = (await SendOkAsync<List<HourTypeDto>>(HttpMethod.Get, "/api/hourtypes", employerToken)).First(t => !t.IsLeave);

        // Hours logged, changed and removed on Eva's behalf are recorded; her own changes aren't.
        var entry = await SendOkAsync<TimeEntryDto>(HttpMethod.Post, "/api/timeentries", employerToken, new {
            TargetUserId = employee.Id, HourTypeId = work.Id, WorkDate = "2025-09-10", StartTime = "09:00", EndTime = "17:00", BreakMinutes = 30
        });
        await SendOkAsync<TimeEntryDto>(HttpMethod.Put, $"/api/timeentries/{entry.Id}", employerToken, new {
            Id = entry.Id, HourTypeId = work.Id, WorkDate = "2025-09-10", StartTime = "09:00", EndTime = "16:00", BreakMinutes = 30
        });
        var own = await SendOkAsync<TimeEntryDto>(HttpMethod.Post, "/api/timeentries", employeeToken, new {
            HourTypeId = work.Id, WorkDate = "2025-09-11", StartTime = "09:00", EndTime = "17:00", BreakMinutes = 30
        });
        (await StatusAsync(HttpMethod.Delete, $"/api/timeentries/{own.Id}", employeeToken)).Should().Be(HttpStatusCode.NoContent);
        (await StatusAsync(HttpMethod.Delete, $"/api/timeentries/{entry.Id}", employerToken)).Should().Be(HttpStatusCode.NoContent);

        await SendOkAsync<object>(HttpMethod.Put, "/api/locked-months/2025/9", employerToken);
        (await StatusAsync(HttpMethod.Delete, "/api/locked-months/2025/9", employerToken)).Should().Be(HttpStatusCode.NoContent);
        (await StatusAsync(HttpMethod.Post, $"/api/users/{employee.Id}/reset-password", employerToken,
            new { NewPassword = "Temporary123!" })).Should().Be(HttpStatusCode.NoContent);
        (await StatusAsync(HttpMethod.Delete, $"/api/users/{employee.Id}", employerToken)).Should().Be(HttpStatusCode.NoContent);
        (await StatusAsync(HttpMethod.Post, $"/api/users/{employee.Id}/activate", employerToken)).Should().Be(HttpStatusCode.NoContent);

        // A platform admin's reset shows up in the company's own log.
        var adminToken = await LoginAsAdminAsync(_client);
        var employers = await SendOkAsync<List<TenantEmployerDto>>(HttpMethod.Get, $"/api/tenants/{tenant.Tenant.Id}/employers", adminToken);
        var secondEmployerEmail = $"{Guid.NewGuid()}@employer.local";
        var secondEmployer = await SendOkAsync<UserDto>(HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = "Sam", LastName = "Second", Email = secondEmployerEmail, Password, Role = "Employer"
        });
        (await StatusAsync(HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/employers/{secondEmployer.Id}/reset-password",
            adminToken, new { NewPassword = "Temporary123!" })).Should().Be(HttpStatusCode.NoContent);
        employers.Should().NotBeEmpty();

        var log = await SendOkAsync<List<AuditEntryDto>>(HttpMethod.Get, "/api/audit-log", employerToken);
        log.Select(e => e.Action).Should().Equal(
            "PasswordReset", "UserActivated", "UserDeactivated", "PasswordReset", "MonthUnlocked", "MonthLocked",
            "TimeEntryDeleted", "TimeEntryUpdated", "TimeEntryCreated");

        log[0].Should().Match<AuditEntryDto>(e => e.ActorRole == "PlatformAdmin" && e.ActorName == "Test Admin"
                                                  && e.SubjectName == "Sam Second");
        log[1].Should().Match<AuditEntryDto>(e => e.ActorName == "Test Employer" && e.SubjectName == "Eva Employee");
        log[4].Details["month"].Should().Be("2025-09");
        var updated = log.Single(e => e.Action == "TimeEntryUpdated");
        updated.Details["before"].Should().Be("2025-09-10 09:00–17:00, 30 min break, Work");
        updated.Details["after"].Should().Be("2025-09-10 09:00–16:00, 30 min break, Work");

        // Paging: the next page starts after the last entry seen.
        var firstPage = await SendOkAsync<List<AuditEntryDto>>(HttpMethod.Get, "/api/audit-log?take=4", employerToken);
        var before = Uri.EscapeDataString(firstPage[^1].AtUtc.ToString("O"));
        var nextPage = await SendOkAsync<List<AuditEntryDto>>(HttpMethod.Get, $"/api/audit-log?take=4&before={before}", employerToken);
        firstPage.Concat(nextPage).Select(e => e.Id).Should().Equal(log.Take(8).Select(e => e.Id));

        (await SendOkAsync<List<AuditEntryDto>>(HttpMethod.Get, "/api/audit-log", otherToken)).Should().BeEmpty();
        var employeeAgain = await LoginAsync(_client, email, "Temporary123!");
        (await StatusAsync(HttpMethod.Get, "/api/audit-log", employeeAgain)).Should().Be(HttpStatusCode.Forbidden);
    }
}

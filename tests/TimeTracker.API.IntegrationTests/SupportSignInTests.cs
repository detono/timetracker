using System.Net;
using FluentAssertions;
using TimeTracker.Application.AuditLog;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Application.Tenants.Dtos;
using TimeTracker.Application.Users.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>A platform admin signing in as one of a company's employers, to help them out.</summary>
public class SupportSignInTests : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client;

    public SupportSignInTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private async Task<Guid> EmployerIdAsync(string adminToken, Guid tenantId) =>
        (await SendOkAsync<List<TenantEmployerDto>>(_client, HttpMethod.Get, $"/api/tenants/{tenantId}/employers", adminToken))
        .Single().Id;

    private Task<HttpResponseMessage> SignInAsAsync(string adminToken, Guid tenantId, Guid userId) =>
        SendAsync(_client, HttpMethod.Post, $"/api/tenants/{tenantId}/employers/{userId}/sign-in", adminToken);

    [Fact]
    public async Task Admin_ActsAsTheEmployer_AndTheCompanySeesWhoDidWhat() {
        var tenant = await CreateTenantAsync(_client);
        var adminToken = await LoginAsAdminAsync(_client);
        var employerId = await EmployerIdAsync(adminToken, tenant.Tenant.Id);

        var session = await SendOkAsync<AuthResultDto>(
            _client, HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/employers/{employerId}/sign-in", adminToken);

        session.UserId.Should().Be(employerId);
        session.Role.Should().Be("Employer");
        session.TenantName.Should().Be(tenant.Tenant.Name);
        session.RefreshToken.Should().BeNull();

        // Works on employer-only pages, like webhooks.
        (await SendAsync(_client, HttpMethod.Post, "/api/webhooks", session.Token, new {
            Name = "Payroll", Url = "https://receiver.example/support", Events = new[] { "leave.approved" }, IsEnabled = true
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var log = await SendOkAsync<List<AuditEntryDto>>(_client, HttpMethod.Get, "/api/audit-log", employerToken);
        log.Select(e => (e.Action, e.ActorName, e.ActorRole)).Should().ContainInOrder(
            ("WebhookCreated", "Test Admin", "PlatformAdmin"),
            ("SupportSignIn", "Test Admin", "PlatformAdmin"));
        log.Single(e => e.Action == "SupportSignIn").SubjectName.Should().Be("Test Employer");
    }

    [Fact]
    public async Task HoursSupportLogsForTheEmployerThemself_AreRecordedToo() {
        var tenant = await CreateTenantAsync(_client);
        var adminToken = await LoginAsAdminAsync(_client);
        var employerId = await EmployerIdAsync(adminToken, tenant.Tenant.Id);
        var session = await SendOkAsync<AuthResultDto>(
            _client, HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/employers/{employerId}/sign-in", adminToken);
        var work = (await SendOkAsync<List<HourTypeDto>>(_client, HttpMethod.Get, "/api/hourtypes", session.Token))
            .First(t => !t.IsLeave);

        await SendOkAsync<TimeEntryDto>(_client, HttpMethod.Post, "/api/timeentries", session.Token, new {
            HourTypeId = work.Id, WorkDate = "2025-09-10", StartTime = "09:00", EndTime = "17:00", BreakMinutes = 30
        });

        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var log = await SendOkAsync<List<AuditEntryDto>>(_client, HttpMethod.Get, "/api/audit-log", employerToken);
        log[0].Should().Match<AuditEntryDto>(e =>
            e.Action == "TimeEntryCreated" && e.ActorRole == "PlatformAdmin" && e.SubjectName == "Test Employer");
    }

    [Fact]
    public async Task Admin_CanSignInAsAnEmployerWhoStillHasATemporaryPassword() {
        var tenant = await CreateTenantAsync(_client);
        var adminToken = await LoginAsAdminAsync(_client);
        var employerId = await EmployerIdAsync(adminToken, tenant.Tenant.Id);
        (await SendAsync(_client, HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/employers/{employerId}/reset-password",
            adminToken, new { NewPassword = "Temporary-1" })).IsSuccessStatusCode.Should().BeTrue();

        var session = await SendOkAsync<AuthResultDto>(
            _client, HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/employers/{employerId}/sign-in", adminToken);

        session.MustChangePassword.Should().BeFalse();
        (await SendAsync(_client, HttpMethod.Get, "/api/webhooks", session.Token)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OnlyEmployersOfThatCompany_AndOnlyByAPlatformAdmin() {
        var tenant = await CreateTenantAsync(_client);
        var other = await CreateTenantAsync(_client);
        var adminToken = await LoginAsAdminAsync(_client);
        var employerId = await EmployerIdAsync(adminToken, tenant.Tenant.Id);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(_client, employerToken);

        (await SignInAsAsync(adminToken, tenant.Tenant.Id, employee.Id)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SignInAsAsync(adminToken, other.Tenant.Id, employerId)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SignInAsAsync(employerToken, tenant.Tenant.Id, employerId)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NotForADeactivatedCompanyOrEmployer() {
        var tenant = await CreateTenantAsync(_client);
        var adminToken = await LoginAsAdminAsync(_client);
        var employerId = await EmployerIdAsync(adminToken, tenant.Tenant.Id);

        (await SendAsync(_client, HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/deactivate", adminToken))
            .IsSuccessStatusCode.Should().BeTrue();

        (await SignInAsAsync(adminToken, tenant.Tenant.Id, employerId)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SupportCannotChangeTheEmployersPasswordOrLanguage() {
        var tenant = await CreateTenantAsync(_client);
        var adminToken = await LoginAsAdminAsync(_client);
        var employerId = await EmployerIdAsync(adminToken, tenant.Tenant.Id);
        var session = await SendOkAsync<AuthResultDto>(
            _client, HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/employers/{employerId}/sign-in", adminToken);

        (await SendAsync(_client, HttpMethod.Post, "/api/auth/change-password", session.Token,
            new { CurrentPassword = Password, NewPassword = "Hijacked-Password-1" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(_client, HttpMethod.Put, "/api/auth/language", session.Token, new { Language = "fr" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The employer's own sign-in still works with their own password.
        await LoginAsync(_client, tenant.EmployerEmail, Password);
    }

    [Fact]
    public async Task DeactivatingTheAdmin_EndsTheirSupportSessionRightAway() {
        var tenant = await CreateTenantAsync(_client);
        var seededAdminToken = await LoginAsAdminAsync(_client);
        var email = $"{Guid.NewGuid()}@admin.local";
        var admin = await SendOkAsync<UserDto>(_client, HttpMethod.Post, "/api/platform-admins", seededAdminToken,
            new { FirstName = "Sam", LastName = "Support", Email = email, Password });
        var adminToken = await LoginAsync(_client, email, Password);
        var employerId = await EmployerIdAsync(adminToken, tenant.Tenant.Id);
        var session = await SendOkAsync<AuthResultDto>(
            _client, HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/employers/{employerId}/sign-in", adminToken);
        (await SendAsync(_client, HttpMethod.Get, "/api/webhooks", session.Token)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await SendAsync(_client, HttpMethod.Post, $"/api/platform-admins/{admin.Id}/deactivate", seededAdminToken))
            .IsSuccessStatusCode.Should().BeTrue();

        (await SendAsync(_client, HttpMethod.Get, "/api/webhooks", session.Token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Tenants.Dtos;
using TimeTracker.Application.Users.Commands.RegisterUser;
using TimeTracker.Application.Users.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>Keeps every email instead of sending it, so tests can read the reset code.</summary>
public class RecordingMailDispatcher : IMailDispatcher {
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    public Task DispatchAsync(TenantMailSettings settings, EmailMessage message, CancellationToken cancellationToken = default) {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }
}

public class MailRecordingFactory : CustomWebApplicationFactory {
    public RecordingMailDispatcher Mail { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => {
            services.RemoveAll<IMailDispatcher>();
            services.AddSingleton<IMailDispatcher>(Mail);
        });
    }
}

public class AccountSecurityTests : IClassFixture<MailRecordingFactory> {
    private const string ProtectedUrl = "/api/hourtypes";

    private readonly MailRecordingFactory _factory;
    private readonly HttpClient _client;

    public AccountSecurityTests(MailRecordingFactory factory) {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) {
        using var request = Authorized(method, url, token);
        if (body is not null) {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private async Task<(string Email, Guid Id)> CreateEmployeeAsync(string employerToken) {
        var email = $"{Guid.NewGuid()}@employee.local";
        var response = await SendAsync(HttpMethod.Post, "/api/users", employerToken,
            new RegisterUserCommand("Eva", "Employee", email, Password, UserRole.Employee));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = (await response.Content.ReadFromJsonAsync<UserDto>())!;
        return (email, user.Id);
    }

    private async Task<AuthResultDto> LoginResultAsync(string email, string password) {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private async Task EnableEmailAsync(string employerToken) {
        var response = await SendAsync(HttpMethod.Put, "/api/mail-settings", employerToken, new {
            Provider = "Smtp", IsEnabled = true, FromAddress = "noreply@a.test", FromName = "A",
            SmtpHost = "smtp.a.test", SmtpPort = 587, SmtpSecurity = "StartTls", SmtpUsername = "user",
            SmtpPassword = "smtp-password-123"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private string LatestCodeFor(string email) {
        var message = _factory.Mail.Sent.Last(m => m.To.Contains(email));
        return Regex.Match(message.TextBody, @"\b[A-Z2-9]{4}-[A-Z2-9]{4}\b").Value;
    }

    [Fact]
    public async Task DeactivatedUser_IsSignedOutRightAway() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(employerToken);
        var employeeToken = await LoginAsync(_client, employee.Email, Password);
        (await SendAsync(HttpMethod.Get, ProtectedUrl, employeeToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await SendAsync(HttpMethod.Delete, $"/api/users/{employee.Id}", employerToken)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await SendAsync(HttpMethod.Get, ProtectedUrl, employeeToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivatedCompany_SignsOutEveryone_AndReactivatingLetsThemBackIn() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var adminToken = await LoginAsAdminAsync(_client);

        (await SendAsync(HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/deactivate", adminToken)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(HttpMethod.Get, ProtectedUrl, employerToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await SendAsync(HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/activate", adminToken)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(HttpMethod.Get, ProtectedUrl, employerToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EmployerReset_SignsOutAndRequiresANewPasswordBeforeAnythingElse() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(employerToken);
        var oldToken = await LoginAsync(_client, employee.Email, Password);

        (await SendAsync(HttpMethod.Post, $"/api/users/{employee.Id}/reset-password", employerToken,
            new { NewPassword = "Temporary123!" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await SendAsync(HttpMethod.Get, ProtectedUrl, oldToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var temporary = await LoginResultAsync(employee.Email, "Temporary123!");
        temporary.MustChangePassword.Should().BeTrue();
        var blocked = await SendAsync(HttpMethod.Get, ProtectedUrl, temporary.Token);
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await blocked.Content.ReadAsStringAsync()).Should().Contain("passwordChangeRequired");

        // The temporary password isn't asked again, but it can't be kept.
        (await SendAsync(HttpMethod.Post, "/api/auth/change-password", temporary.Token,
            new { NewPassword = "Temporary123!" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var changed = await SendAsync(HttpMethod.Post, "/api/auth/change-password", temporary.Token,
            new { NewPassword = "MyOwnPassword1!" });
        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        var fresh = (await changed.Content.ReadFromJsonAsync<AuthResultDto>())!;
        fresh.MustChangePassword.Should().BeFalse();

        (await SendAsync(HttpMethod.Get, ProtectedUrl, fresh.Token)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAsync(HttpMethod.Get, ProtectedUrl, temporary.Token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NewAccount_MustChooseItsOwnPasswordAtFirstLogin() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(employerToken);

        var first = await LoginResultAsync(employee.Email, Password);

        first.MustChangePassword.Should().BeTrue();
        (await SendAsync(HttpMethod.Get, ProtectedUrl, first.Token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangingYourOwnPassword_SignsOutYourOtherSessions() {
        var tenant = await CreateTenantAsync(_client);
        var laptop = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var phone = await LoginAsync(_client, tenant.EmployerEmail, Password);

        var changed = await SendAsync(HttpMethod.Post, "/api/auth/change-password", laptop,
            new { CurrentPassword = Password, NewPassword = "Different123!" });
        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        var fresh = (await changed.Content.ReadFromJsonAsync<AuthResultDto>())!;

        (await SendAsync(HttpMethod.Get, ProtectedUrl, phone)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await SendAsync(HttpMethod.Get, ProtectedUrl, fresh.Token)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlatformAdmin_ResetsAnEmployer_ButNotEmployeesOrOtherCompanies() {
        var tenant = await CreateTenantAsync(_client);
        var other = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(employerToken);
        var adminToken = await LoginAsAdminAsync(_client);

        var list = await SendAsync(HttpMethod.Get, $"/api/tenants/{tenant.Tenant.Id}/employers", adminToken);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var employers = (await list.Content.ReadFromJsonAsync<List<TenantEmployerDto>>())!;
        employers.Should().ContainSingle().Which.Email.Should().Be(tenant.EmployerEmail);
        var employerId = employers[0].Id;

        // Employees, and employers of another company, are out of reach.
        (await SendAsync(HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/employers/{employee.Id}/reset-password",
            adminToken, new { NewPassword = "Temporary123!" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(HttpMethod.Post, $"/api/tenants/{other.Tenant.Id}/employers/{employerId}/reset-password",
            adminToken, new { NewPassword = "Temporary123!" })).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await SendAsync(HttpMethod.Post, $"/api/tenants/{tenant.Tenant.Id}/employers/{employerId}/reset-password",
            adminToken, new { NewPassword = "Temporary123!" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await SendAsync(HttpMethod.Get, ProtectedUrl, employerToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await LoginResultAsync(tenant.EmployerEmail, "Temporary123!")).MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task TenantUsers_CannotUseThePlatformAdminEndpoints() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);

        (await SendAsync(HttpMethod.Get, $"/api/tenants/{tenant.Tenant.Id}/employers", employerToken)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ForgotPassword_EmailsACode_ThatSetsANewPasswordOnce() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        await EnableEmailAsync(employerToken);
        var employee = await CreateEmployeeAsync(employerToken);
        var oldToken = await LoginAsync(_client, employee.Email, Password);

        var availability = await _client.GetFromJsonAsync<Dictionary<string, bool>>("/api/auth/password-reset");
        availability!["available"].Should().BeTrue();

        (await _client.PostAsJsonAsync("/api/auth/password-reset", new { Email = employee.Email })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        var code = LatestCodeFor(employee.Email);
        code.Should().NotBeEmpty();

        // Lowercase, without the dash, still works.
        var normalized = code.Replace("-", " ").ToLowerInvariant();
        (await _client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new { Email = employee.Email, Code = normalized, NewPassword = "Remembered123!" })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await SendAsync(HttpMethod.Get, ProtectedUrl, oldToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await LoginResultAsync(employee.Email, "Remembered123!")).MustChangePassword.Should().BeFalse();

        // A code works only once.
        (await _client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new { Email = employee.Email, Code = code, NewPassword = "AgainAgain123!" })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_StopsAfterFiveWrongCodes() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        await EnableEmailAsync(employerToken);
        var employee = await CreateEmployeeAsync(employerToken);

        await _client.PostAsJsonAsync("/api/auth/password-reset", new { Email = employee.Email });
        var code = LatestCodeFor(employee.Email);

        for (var i = 0; i < PasswordResetCode.MaxFailedAttempts; i++) {
            (await _client.PostAsJsonAsync("/api/auth/password-reset/confirm",
                new { Email = employee.Email, Code = "WRONG-CODE", NewPassword = "Guessing123!" })).StatusCode
                .Should().Be(HttpStatusCode.BadRequest);
        }

        // Even the right code no longer works.
        (await _client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new { Email = employee.Email, Code = code, NewPassword = "Remembered123!" })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_SendsNothing_ForUnknownAddressesOrCompaniesWithoutEmail() {
        var tenant = await CreateTenantAsync(_client);
        var before = _factory.Mail.Sent.Count;

        (await _client.PostAsJsonAsync("/api/auth/password-reset", new { Email = "nobody@nowhere.local" })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await _client.PostAsJsonAsync("/api/auth/password-reset", new { Email = tenant.EmployerEmail })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await _client.PostAsJsonAsync("/api/auth/password-reset", new { Email = CustomWebApplicationFactory.AdminEmail }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        _factory.Mail.Sent.Count.Should().Be(before);
    }

    [Fact]
    public async Task ForgotPassword_SendsOneCodePerMinute() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        await EnableEmailAsync(employerToken);

        await _client.PostAsJsonAsync("/api/auth/password-reset", new { Email = tenant.EmployerEmail });
        await _client.PostAsJsonAsync("/api/auth/password-reset", new { Email = tenant.EmployerEmail });

        _factory.Mail.Sent.Count(m => m.To.Contains(tenant.EmployerEmail)).Should().Be(1);
    }

    [Fact]
    public async Task FiveWrongPasswords_LockTheAccount_UntilItsPasswordIsReset() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        await EnableEmailAsync(employerToken);
        var employee = await CreateEmployeeAsync(employerToken);
        await LoginAsync(_client, employee.Email, Password);

        for (var i = 0; i < 5; i++) {
            (await _client.PostAsJsonAsync("/api/auth/login", new { Email = employee.Email, Password = "Guess123!" }))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // Now even the right password is refused, the same way an unknown address would be.
        var locked = await _client.PostAsJsonAsync("/api/auth/login", new { Email = employee.Email, Password });
        locked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await locked.Content.ReadAsStringAsync()).Should().Contain("Try again in 15 minutes");

        // Resetting the password lifts the lock.
        await _client.PostAsJsonAsync("/api/auth/password-reset", new { Email = employee.Email });
        (await _client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new { Email = employee.Email, Code = LatestCodeFor(employee.Email), NewPassword = "Remembered123!" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LoginResultAsync(employee.Email, "Remembered123!")).Token.Should().NotBeEmpty();
    }
}

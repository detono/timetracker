using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>The real clock, moved forward by <see cref="Offset"/>.</summary>
public class ShiftableClock : IDateTimeProvider {
    public TimeSpan Offset { get; set; }
    public DateTime UtcNow => DateTime.UtcNow + Offset;
    public DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow);
}

public class ShiftableClockFactory : CustomWebApplicationFactory {
    public ShiftableClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => {
            services.RemoveAll<IDateTimeProvider>();
            services.AddSingleton<IDateTimeProvider>(Clock);
        });
    }
}

/// <summary>Each test moves the clock, so each gets its own server.</summary>
public class RefreshTokenTests : IDisposable {
    private const string ProtectedUrl = "/api/hourtypes";

    private readonly ShiftableClockFactory _factory = new();
    private readonly HttpClient _client;

    public RefreshTokenTests() {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private async Task<(Guid Id, string Email, string EmployerToken)> NewEmployeeAsync() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(_client, employerToken);
        return (employee.Id, employee.Email, employerToken);
    }

    private async Task<AuthResultDto> MobileLoginAsync(string email) {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password, IssueRefreshToken = true });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });

    private async Task<AuthResultDto> RefreshOkAsync(string refreshToken) {
        var response = await RefreshAsync(refreshToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    [Fact]
    public async Task WebLogin_GetsNoRefreshToken() {
        var employee = await NewEmployeeAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { employee.Email, Password });

        (await response.Content.ReadFromJsonAsync<AuthResultDto>())!.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_GivesWorkingTokenAndNewRefreshToken() {
        var employee = await NewEmployeeAsync();
        var login = await MobileLoginAsync(employee.Email);
        login.RefreshToken.Should().NotBeNullOrEmpty();

        var refreshed = await RefreshOkAsync(login.RefreshToken!);

        refreshed.RefreshToken.Should().NotBeNullOrEmpty().And.NotBe(login.RefreshToken);
        refreshed.UserId.Should().Be(employee.Id);
        (await SendAsync(_client, HttpMethod.Get, ProtectedUrl, refreshed.Token)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RetryWithinGracePeriod_StillWorks() {
        // The phone sent the refresh, the answer got lost, and it tries again with the same token.
        var employee = await NewEmployeeAsync();
        var login = await MobileLoginAsync(employee.Email);
        await RefreshOkAsync(login.RefreshToken!);

        var retried = await RefreshOkAsync(login.RefreshToken!);

        await RefreshOkAsync(retried.RefreshToken!);
    }

    [Fact]
    public async Task ReplayedToken_RevokesTheWholeSignIn() {
        var employee = await NewEmployeeAsync();
        var login = await MobileLoginAsync(employee.Email);
        var refreshed = await RefreshOkAsync(login.RefreshToken!);

        _factory.Clock.Offset = RefreshToken.ReuseGracePeriod + TimeSpan.FromMinutes(1);
        (await RefreshAsync(login.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // The legitimate device's newer token was revoked too: whoever copied it gets nothing more.
        (await RefreshAsync(refreshed.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OtherDevices_AreNotAffectedByAReplay() {
        var employee = await NewEmployeeAsync();
        var phone = await MobileLoginAsync(employee.Email);
        var tablet = await MobileLoginAsync(employee.Email);
        await RefreshOkAsync(phone.RefreshToken!);

        _factory.Clock.Offset = RefreshToken.ReuseGracePeriod + TimeSpan.FromMinutes(1);
        (await RefreshAsync(phone.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await RefreshOkAsync(tablet.RefreshToken!);
    }

    [Fact]
    public async Task UnusedToken_ExpiresAfterConfiguredDays() {
        var employee = await NewEmployeeAsync();
        var login = await MobileLoginAsync(employee.Email);

        _factory.Clock.Offset = TimeSpan.FromDays(14);

        (await RefreshAsync(login.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken() {
        var employee = await NewEmployeeAsync();
        var login = await MobileLoginAsync(employee.Email);

        var response = await _client.PostAsJsonAsync("/api/auth/logout", new { login.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await RefreshAsync(login.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_StillSucceeds() {
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new { RefreshToken = "not-a-real-token" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeactivatedUser_CannotRefresh() {
        var employee = await NewEmployeeAsync();
        var login = await MobileLoginAsync(employee.Email);

        (await SendAsync(_client, HttpMethod.Delete, $"/api/users/{employee.Id}", employee.EmployerToken)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await RefreshAsync(login.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PasswordReset_EndsRefreshTokens() {
        var employee = await NewEmployeeAsync();
        var login = await MobileLoginAsync(employee.Email);

        (await SendAsync(_client, HttpMethod.Post, $"/api/users/{employee.Id}/reset-password", employee.EmployerToken,
            new { NewPassword = "Another-Password-1" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await RefreshAsync(login.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangingPassword_EndsOldRefreshToken_AndCanIssueANewOne() {
        var employee = await NewEmployeeAsync();
        var login = await MobileLoginAsync(employee.Email);

        var changed = await SendOkAsync<AuthResultDto>(_client, HttpMethod.Post, "/api/auth/change-password", login.Token,
            new { CurrentPassword = Password, NewPassword = "Another-Password-1", IssueRefreshToken = true });

        (await RefreshAsync(login.RefreshToken!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        changed.RefreshToken.Should().NotBeNullOrEmpty();
        await RefreshOkAsync(changed.RefreshToken!);
    }

    [Fact]
    public async Task PlatformAdmin_CanRefresh() {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new {
            Email = CustomWebApplicationFactory.AdminEmail,
            Password = CustomWebApplicationFactory.AdminPassword,
            IssueRefreshToken = true
        });
        var login = (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;

        var refreshed = await RefreshOkAsync(login.RefreshToken!);

        refreshed.Role.Should().Be("PlatformAdmin");
    }

    [Fact]
    public async Task Refresh_WithGarbage_IsRejected() {
        (await RefreshAsync("garbage")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await RefreshAsync("")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

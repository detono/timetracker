using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Application.MailSettings.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

public class MailSettingsTests : IClassFixture<CustomWebApplicationFactory> {
    // The API serializes enums as strings.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MailSettingsTests(CustomWebApplicationFactory factory) {
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

    private async Task<MailSettingsDto> GetSettingsAsync(string token) {
        var response = await SendAsync(HttpMethod.Get, "/api/mail-settings", token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<MailSettingsDto>(Json))!;
    }

    private static object SmtpSettings(string host, int port = 587, string? password = "smtp-password-123") => new {
        Provider = "Smtp", IsEnabled = true, FromAddress = "noreply@a.test", FromName = "A",
        SmtpHost = host, SmtpPort = port, SmtpSecurity = "StartTls", SmtpUsername = "user", SmtpPassword = password
    };

    [Fact]
    public async Task TenantsCannotReadOrChangeEachOthersMailSettings() {
        var tenantA = await CreateTenantAsync(_client);
        var tenantB = await CreateTenantAsync(_client);
        var tokenA = await LoginAsync(_client, tenantA.EmployerEmail, Password);
        var tokenB = await LoginAsync(_client, tenantB.EmployerEmail, Password);

        (await SendAsync(HttpMethod.Put, "/api/mail-settings", tokenA, SmtpSettings("smtp.a.test"))).StatusCode
            .Should().Be(HttpStatusCode.OK);

        // B sees nothing of A's settings...
        (await GetSettingsAsync(tokenB)).IsConfigured.Should().BeFalse();

        // ...and saving its own creates a separate record that leaves A's untouched.
        (await SendAsync(HttpMethod.Put, "/api/mail-settings", tokenB, SmtpSettings("smtp.b.test", 2525))).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var settingsA = await GetSettingsAsync(tokenA);
        settingsA.SmtpHost.Should().Be("smtp.a.test");
        settingsA.SmtpPort.Should().Be(587);
        (await GetSettingsAsync(tokenB)).SmtpHost.Should().Be("smtp.b.test");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rows = await db.MailSettings.IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantA.Tenant.Id || s.TenantId == tenantB.Tenant.Id).ToListAsync();
        rows.Should().HaveCount(2);
        rows.Select(r => r.TenantId).Should().BeEquivalentTo(new[] { tenantA.Tenant.Id, tenantB.Tenant.Id });
    }

    [Fact]
    public async Task SecretsAreEncryptedAtRestAndNeverReturned() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);

        var response = await SendAsync(HttpMethod.Put, "/api/mail-settings", token, SmtpSettings("smtp.a.test"));
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("smtp-password-123");

        // Saving again with an empty password keeps the stored one.
        (await SendAsync(HttpMethod.Put, "/api/mail-settings", token, SmtpSettings("smtp.a.test", password: ""))).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var settings = await GetSettingsAsync(token);
        settings.HasSmtpPassword.Should().BeTrue();
        settings.Provider.Should().Be(MailProvider.Smtp);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await db.MailSettings.IgnoreQueryFilters().SingleAsync(s => s.TenantId == tenant.Tenant.Id);
        row.SmtpPasswordProtected.Should().NotBeNullOrEmpty().And.NotContain("smtp-password-123");
    }

    [Fact]
    public async Task EmployeesCannotReachMailSettings() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employeeEmail = $"{Guid.NewGuid()}@employee.local";
        (await SendAsync(HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = "Emma", LastName = "Ployee", Email = employeeEmail, Password, Role = "Employee"
        })).IsSuccessStatusCode.Should().BeTrue();
        var employeeToken = await LoginAsync(_client, employeeEmail, Password);

        (await SendAsync(HttpMethod.Get, "/api/mail-settings", employeeToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Put, "/api/mail-settings", employeeToken, SmtpSettings("x.test"))).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Post, "/api/mail-settings/test", employeeToken)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestEmail_UnreachableHost_ReturnsReadableError() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var closedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        (await SendAsync(HttpMethod.Put, "/api/mail-settings", token, SmtpSettings("127.0.0.1", closedPort))).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var response = await SendAsync(HttpMethod.Post, "/api/mail-settings/test", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain($"Could not connect to 127.0.0.1:{closedPort}");
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.IdentityModel.Tokens;
using TimeTracker.API.Authorization;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Security;
using TimeTracker.Infrastructure.Identity;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

public class SecurityHardeningTests : IClassFixture<CustomWebApplicationFactory> {
    private const string TestSecret = "integration-tests-only-secret-0123456789abcdef";
    private readonly HttpClient _client;

    public SecurityHardeningTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    /// <summary>A correctly signed token, as someone who knows the secret could make one.</summary>
    private static string Forge(Guid userId, string role, Guid? tenantId, int sessionVersion = 2) {
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new(SessionClaimTypes.SessionVersion, sessionVersion.ToString())
        };
        if (tenantId is { } id) claims.Add(new Claim(TenantClaimTypes.TenantId, id.ToString()));

        var token = new JwtSecurityToken("TimeTracker.API", "TimeTracker.Client", claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task ATokenClaimingAnotherRoleOrCompany_IsRefused_EvenWhenCorrectlySigned() {
        var tenant = await CreateTenantAsync(_client);
        var other = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(_client, employerToken);

        // Control: the account's true role and company are accepted (session version 2, because the
        // test helper changed the first password twice), so the refusals below are about the claims.
        (await SendAsync(_client, HttpMethod.Get, "/api/hourtypes", Forge(employee.Id, "Employee", tenant.Tenant.Id)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await SendAsync(_client, HttpMethod.Get, "/api/hourtypes", Forge(employee.Id, "Employer", tenant.Tenant.Id)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await SendAsync(_client, HttpMethod.Get, "/api/hourtypes", Forge(employee.Id, "Employee", other.Tenant.Id)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await SendAsync(_client, HttpMethod.Get, "/api/tenants", Forge(employee.Id, "PlatformAdmin", null)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TheBootstrapAdmin_MustChooseItsOwnPassword_AndHealthNeedsNoSignIn() {
        var first = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.AdminEmail, Password = CustomWebApplicationFactory.AdminPassword });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await first.Content.ReadFromJsonAsync<AuthResultDto>())!.MustChangePassword.Should().BeTrue();

        (await _client.GetAsync("/health")).StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        (await _client.GetAsync("/api/hourtypes")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "Production", false)]
    [InlineData("too-short", "Production", false)]
    [InlineData("REPLACE_WITH_A_LONG_RANDOM_SECRET_IN_PRODUCTION_ENV_VARS", "Production", false)]
    [InlineData("change_me_to_a_long_random_string", "Production", false)]
    [InlineData(JwtSettingsValidator.DevelopmentSecret, "Production", false)]
    [InlineData(JwtSettingsValidator.DevelopmentSecret, "Development", true)]
    [InlineData("q8Zr0x5K2mVt9Lw3Np7Ys1Bc6Hd4Fg0Jk2Qe8Ru5Ti", "Production", true)]
    public void TheSigningSecret_MustBeLongAndNotPublic(string secret, string environment, bool valid) {
        var validator = new JwtSettingsValidator(new HostingEnvironment { EnvironmentName = environment });

        validator.Validate(null, new JwtSettings { Secret = secret }).Succeeded.Should().Be(valid);
    }
}

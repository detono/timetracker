using System.Net;
using FluentAssertions;
using TimeTracker.Application.Company.Dtos;
using TimeTracker.Application.Tenants.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>The company list for platform admins, and company details for employers.</summary>
public class AdminReadTests : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client;

    public AdminReadTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PlatformAdminsListCompaniesWithTheirSize_OthersCant() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        await CreateEmployeeAsync(_client, employerToken);
        var adminToken = await LoginAsAdminAsync(_client);

        var tenants = await SendOkAsync<List<TenantDto>>(_client, HttpMethod.Get, "/api/tenants", adminToken);
        tenants.Single(t => t.Id == tenant.Tenant.Id).Should().Match<TenantDto>(t => t.UserCount == 2 && t.IsActive);

        (await SendAsync(_client, HttpMethod.Get, "/api/tenants", employerToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OnlyEmployersReadTheirCompanyDetails() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(_client, employerToken);
        var adminToken = await LoginAsAdminAsync(_client);

        (await SendOkAsync<CompanyDto>(_client, HttpMethod.Get, "/api/company", employerToken)).Name.Should().Be(tenant.Tenant.Name);
        (await SendAsync(_client, HttpMethod.Get, "/api/company", employee.Token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(_client, HttpMethod.Get, "/api/company", adminToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

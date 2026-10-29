using System.Net;
using FluentAssertions;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.Projects.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

public class ProjectManagementTests : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client;

    public ProjectManagementTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private Task<List<ProjectDto>> ListAsync(string token, bool includeInactive = false) =>
        SendOkAsync<List<ProjectDto>>(_client, HttpMethod.Get, $"/api/projects?includeInactive={includeInactive}", token);

    private async Task LogAsync(string token, Guid hourTypeId, Guid projectId, DateOnly date, string end) =>
        (await SendAsync(_client, HttpMethod.Post, "/api/timeentries", token, new {
            HourTypeId = hourTypeId, ProjectId = projectId, WorkDate = date.ToString("yyyy-MM-dd"),
            StartTime = "09:00", EndTime = end, BreakMinutes = 0
        })).StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact]
    public async Task ProjectsTotalTheirHours_AndBreakThemDownPerPerson() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(_client, token);
        var work = (await SendOkAsync<List<HourTypeDto>>(_client, HttpMethod.Get, "/api/hourtypes", token)).First(t => !t.IsLeave);
        var projectId = await SendOkAsync<Guid>(_client, HttpMethod.Post, "/api/projects", token, new { Name = "Website", ClientName = "Acme" });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastMonth = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        await LogAsync(employee.Token, work.Id, projectId, today, "13:00");      // 4h this month
        await LogAsync(employee.Token, work.Id, projectId, lastMonth, "11:00");  // 2h last month
        await LogAsync(token, work.Id, projectId, today, "10:00");               // 1h this month

        var project = (await ListAsync(token)).Single(p => p.Id == projectId);
        project.Should().Match<ProjectDto>(p =>
            p.Name == "Website" && p.ClientName == "Acme" && p.TotalHoursThisMonth == 5 && p.TotalHoursLifetime == 7);

        var breakdown = await SendOkAsync<List<ProjectBreakdownDto>>(_client, HttpMethod.Get, $"/api/projects/{projectId}/breakdown", token);
        breakdown.Select(b => (b.EmployeeName, b.TotalHours)).Should().Equal(("Eva Employee", 6d), ("Test Employer", 1d));
    }

    [Fact]
    public async Task EmployerRenamesAndArchivesProjects() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var projectId = await SendOkAsync<Guid>(_client, HttpMethod.Post, "/api/projects", token, new { Name = "Old name", ClientName = (string?)null });

        (await SendAsync(_client, HttpMethod.Put, $"/api/projects/{projectId}", token,
            new { Id = projectId, Name = "New name", ClientName = "Client" })).IsSuccessStatusCode.Should().BeTrue();
        (await SendAsync(_client, HttpMethod.Put, $"/api/projects/{projectId}", token,
            new { Id = projectId, Name = " ", ClientName = "Client" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ListAsync(token)).Single(p => p.Id == projectId).Should()
            .Match<ProjectDto>(p => p.Name == "New name" && p.ClientName == "Client");

        (await SendAsync(_client, HttpMethod.Patch, $"/api/projects/{projectId}/status", token, false)).IsSuccessStatusCode.Should().BeTrue();
        (await ListAsync(token)).Should().NotContain(p => p.Id == projectId);
        (await ListAsync(token, includeInactive: true)).Should().Contain(p => p.Id == projectId && !p.IsActive);

        (await SendAsync(_client, HttpMethod.Patch, $"/api/projects/{projectId}/status", token, true)).IsSuccessStatusCode.Should().BeTrue();
        (await ListAsync(token)).Should().Contain(p => p.Id == projectId && p.IsActive);
    }

    [Fact]
    public async Task OnlyTheCompanysEmployerManagesItsProjects() {
        var tenant = await CreateTenantAsync(_client);
        var other = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var otherToken = await LoginAsync(_client, other.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(_client, token);
        var projectId = await SendOkAsync<Guid>(_client, HttpMethod.Post, "/api/projects", token, new { Name = "Secret", ClientName = (string?)null });

        // Employees can pick projects but not manage them.
        (await ListAsync(employee.Token)).Should().Contain(p => p.Id == projectId);
        (await SendAsync(_client, HttpMethod.Post, "/api/projects", employee.Token, new { Name = "Mine" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(_client, HttpMethod.Get, $"/api/projects/{projectId}/breakdown", employee.Token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Another company doesn't see or reach it.
        (await ListAsync(otherToken)).Should().NotContain(p => p.Id == projectId);
        (await SendAsync(_client, HttpMethod.Put, $"/api/projects/{projectId}", otherToken,
            new { Id = projectId, Name = "Taken", ClientName = (string?)null })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(_client, HttpMethod.Patch, $"/api/projects/{projectId}/status", otherToken, false))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendOkAsync<List<ProjectBreakdownDto>>(_client, HttpMethod.Get, $"/api/projects/{projectId}/breakdown", otherToken))
            .Should().BeEmpty();
    }
}

using System.Net;
using FluentAssertions;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.TimeEntries.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>
/// Who may see and change whose hours: employers everyone in their company, a supervisor their
/// team (read only), everyone else only themselves.
/// </summary>
public class TeamVisibilityTests : IClassFixture<CustomWebApplicationFactory> {
    private const string Day = "2026-10-05";
    private const string Range = "from=2026-10-05&to=2026-10-05";
    private readonly HttpClient _client;

    public TeamVisibilityTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private async Task<TimeEntryDto> LogAsync(string token, Guid hourTypeId) =>
        await SendOkAsync<TimeEntryDto>(_client, HttpMethod.Post, "/api/timeentries", token, new {
            HourTypeId = hourTypeId, WorkDate = Day, StartTime = "09:00", EndTime = "17:00", BreakMinutes = 30
        });

    private async Task<List<string>> TeamNamesAsync(string token) =>
        (await SendOkAsync<List<TimeEntryDto>>(_client, HttpMethod.Get, $"/api/timeentries/team?{Range}", token))
        .Select(e => e.UserFullName).Order().ToList();

    [Fact]
    public async Task EachRoleSeesExactlyTheHoursItMay() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var lead = await CreateEmployeeAsync(_client, employerToken, "Lea", "Lead");
        var member = await CreateEmployeeAsync(_client, employerToken, "Max", "Member");
        var outsider = await CreateEmployeeAsync(_client, employerToken, "Olga", "Outsider");
        (await SendAsync(_client, HttpMethod.Post, $"/api/users/{member.Id}/supervisor", employerToken, lead.Id))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var work = (await SendOkAsync<List<HourTypeDto>>(_client, HttpMethod.Get, "/api/hourtypes", employerToken))
            .First(t => !t.IsLeave);
        await LogAsync(lead.Token, work.Id);
        var memberEntry = await LogAsync(member.Token, work.Id);
        await LogAsync(outsider.Token, work.Id);

        // The team view.
        (await TeamNamesAsync(employerToken)).Should().Equal("Lea Lead", "Max Member", "Olga Outsider");
        (await TeamNamesAsync(lead.Token)).Should().Equal("Lea Lead", "Max Member");
        (await TeamNamesAsync(outsider.Token)).Should().Equal("Olga Outsider");

        // One person's hours: supervision works one way only.
        (await SendAsync(_client, HttpMethod.Get, $"/api/timeentries?userId={member.Id}&{Range}", lead.Token))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAsync(_client, HttpMethod.Get, $"/api/timeentries?userId={outsider.Id}&{Range}", lead.Token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(_client, HttpMethod.Get, $"/api/timeentries?userId={lead.Id}&{Range}", member.Token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // A supervisor may look but not change; the employer may change.
        var edit = new {
            Id = memberEntry.Id, HourTypeId = work.Id, WorkDate = Day, StartTime = "09:00", EndTime = "16:00", BreakMinutes = 30
        };
        (await SendAsync(_client, HttpMethod.Put, $"/api/timeentries/{memberEntry.Id}", lead.Token, edit))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(_client, HttpMethod.Delete, $"/api/timeentries/{memberEntry.Id}", outsider.Token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(_client, HttpMethod.Put, $"/api/timeentries/{memberEntry.Id}", employerToken, edit))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Removing the supervisor ends the lead's view of that person.
        (await SendAsync(_client, HttpMethod.Post, $"/api/users/{member.Id}/supervisor", employerToken, (Guid?)null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await TeamNamesAsync(lead.Token)).Should().Equal("Lea Lead");
    }

    [Fact]
    public async Task NobodySeesAnotherCompanysHours() {
        var tenant = await CreateTenantAsync(_client);
        var other = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var otherEmployerToken = await LoginAsync(_client, other.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(_client, employerToken);
        var work = (await SendOkAsync<List<HourTypeDto>>(_client, HttpMethod.Get, "/api/hourtypes", employerToken))
            .First(t => !t.IsLeave);
        var entry = await LogAsync(employee.Token, work.Id);

        (await TeamNamesAsync(otherEmployerToken)).Should().BeEmpty();
        (await SendAsync(_client, HttpMethod.Get, $"/api/timeentries?userId={employee.Id}&{Range}", otherEmployerToken))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(_client, HttpMethod.Delete, $"/api/timeentries/{entry.Id}", otherEmployerToken))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

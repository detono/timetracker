using System.Net;
using FluentAssertions;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.Projects.Dtos;
using TimeTracker.Application.Reports.Dtos;
using TimeTracker.Application.Reports.Queries.GetProjectHours;
using TimeTracker.Application.TimeEntries.Dtos;
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

    private Task<HttpResponseMessage> TryLogAsync(string token, Guid hourTypeId, Guid projectId, DateOnly date) =>
        SendAsync(_client, HttpMethod.Post, "/api/timeentries", token, new {
            HourTypeId = hourTypeId, ProjectId = projectId, WorkDate = date.ToString("yyyy-MM-dd"),
            StartTime = "09:00", EndTime = "10:00", BreakMinutes = 0
        });

    private async Task<(string Token, Guid WorkId)> EmployerWithWorkTypeAsync() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var work = (await SendOkAsync<List<HourTypeDto>>(_client, HttpMethod.Get, "/api/hourtypes", token)).First(t => !t.IsLeave);
        return (token, work.Id);
    }

    [Fact]
    public async Task ProjectsKeepTheirBudgetAndBillable() {
        var (token, _) = await EmployerWithWorkTypeAsync();
        var projectId = await SendOkAsync<Guid>(_client, HttpMethod.Post, "/api/projects", token,
            new { Name = "Website", ClientName = "Acme", BudgetHours = 120.5m, IsBillable = true });

        (await ListAsync(token)).Single(p => p.Id == projectId).Should().Match<ProjectDto>(p =>
            p.BudgetHours == 120.5m && p.IsBillable && p.MemberIds.Count == 0 && p.CanLog);

        (await SendAsync(_client, HttpMethod.Put, $"/api/projects/{projectId}", token,
            new { Id = projectId, Name = "Website", ClientName = "Acme", BudgetHours = (decimal?)null, IsBillable = false }))
            .IsSuccessStatusCode.Should().BeTrue();
        (await ListAsync(token)).Single(p => p.Id == projectId).Should().Match<ProjectDto>(p =>
            p.BudgetHours == null && !p.IsBillable);

        (await SendAsync(_client, HttpMethod.Post, "/api/projects", token, new { Name = "Zero", BudgetHours = 0 }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SendAsync(_client, HttpMethod.Post, "/api/projects", token, new { Name = "Stranger", MemberIds = new[] { Guid.NewGuid() } }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OnlyChosenPeopleLogOnARestrictedProject() {
        var (token, workId) = await EmployerWithWorkTypeAsync();
        var member = await CreateEmployeeAsync(_client, token, "Mia", "Member");
        var outsider = await CreateEmployeeAsync(_client, token, "Otto", "Outsider");
        var projectId = await SendOkAsync<Guid>(_client, HttpMethod.Post, "/api/projects", token,
            new { Name = "Secret", MemberIds = new[] { member.Id } });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Employers see who's on it, and that they aren't themselves.
        (await ListAsync(token)).Single(p => p.Id == projectId).Should().Match<ProjectDto>(p =>
            p.MemberIds.SequenceEqual(new[] { member.Id }) && !p.CanLog);

        // Employees only get the projects they may use, without the member list.
        (await ListAsync(member.Token)).Should().ContainSingle(p => p.Id == projectId && p.CanLog && p.MemberIds.Count == 0);
        (await ListAsync(outsider.Token)).Should().NotContain(p => p.Id == projectId);

        (await TryLogAsync(member.Token, workId, projectId, today)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await TryLogAsync(outsider.Token, workId, projectId, today)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TryLogAsync(token, workId, projectId, today)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // An outsider can't move an entry onto it either.
        var own = await SendOkAsync<TimeEntryDto>(_client, HttpMethod.Post, "/api/timeentries", outsider.Token, new {
            HourTypeId = workId, WorkDate = today.ToString("yyyy-MM-dd"), StartTime = "09:00", EndTime = "10:00", BreakMinutes = 0
        });
        (await SendAsync(_client, HttpMethod.Put, $"/api/timeentries/{own.Id}", outsider.Token, new {
            Id = own.Id, HourTypeId = workId, ProjectId = projectId, WorkDate = today.ToString("yyyy-MM-dd"),
            StartTime = "09:00", EndTime = "10:00", BreakMinutes = 0
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Taken off the project, the member can still correct what they logged on it before.
        var logged = (await SendOkAsync<List<TimeEntryDto>>(_client, HttpMethod.Get,
            $"/api/timeentries?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}", member.Token)).Single();
        (await SendAsync(_client, HttpMethod.Put, $"/api/projects/{projectId}", token,
            new { Id = projectId, Name = "Secret", MemberIds = new[] { outsider.Id } })).IsSuccessStatusCode.Should().BeTrue();
        (await SendAsync(_client, HttpMethod.Put, $"/api/timeentries/{logged.Id}", member.Token, new {
            Id = logged.Id, HourTypeId = workId, ProjectId = projectId, WorkDate = today.ToString("yyyy-MM-dd"),
            StartTime = "09:00", EndTime = "11:00", BreakMinutes = 0, Notes = "Fixed"
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        // An empty list opens it to everyone again.
        (await SendAsync(_client, HttpMethod.Put, $"/api/projects/{projectId}", token,
            new { Id = projectId, Name = "Secret", MemberIds = Array.Empty<Guid>() })).IsSuccessStatusCode.Should().BeTrue();
        (await TryLogAsync(token, workId, projectId, today)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BreakdownCanBeLimitedToDates() {
        var (token, workId) = await EmployerWithWorkTypeAsync();
        var projectId = await SendOkAsync<Guid>(_client, HttpMethod.Post, "/api/projects", token, new { Name = "Website" });
        await LogAsync(token, workId, projectId, new DateOnly(2026, 3, 10), "13:00"); // 4h
        await LogAsync(token, workId, projectId, new DateOnly(2026, 4, 10), "11:00"); // 2h

        var march = await SendOkAsync<List<ProjectBreakdownDto>>(_client, HttpMethod.Get,
            $"/api/projects/{projectId}/breakdown?from=2026-03-01&to=2026-03-31", token);
        march.Single().TotalHours.Should().Be(4);

        var all = await SendOkAsync<List<ProjectBreakdownDto>>(_client, HttpMethod.Get, $"/api/projects/{projectId}/breakdown", token);
        all.Single().TotalHours.Should().Be(6);

        (await SendAsync(_client, HttpMethod.Get, $"/api/projects/{projectId}/breakdown?from=2026-04-01&to=2026-03-01", token))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReportsShowHoursPerProjectAndFilterOnOne() {
        var (token, workId) = await EmployerWithWorkTypeAsync();
        var website = await SendOkAsync<Guid>(_client, HttpMethod.Post, "/api/projects", token,
            new { Name = "Website", ClientName = "Acme", IsBillable = true });
        var internalWork = await SendOkAsync<Guid>(_client, HttpMethod.Post, "/api/projects", token, new { Name = "Internal" });
        var day = new DateOnly(2026, 3, 10);
        await LogAsync(token, workId, website, day, "13:00");      // 4h
        await LogAsync(token, workId, internalWork, day.AddDays(1), "11:00"); // 2h
        (await SendAsync(_client, HttpMethod.Post, "/api/timeentries", token, new {
            HourTypeId = workId, WorkDate = day.AddDays(2).ToString("yyyy-MM-dd"), StartTime = "09:00", EndTime = "10:00", BreakMinutes = 0
        })).IsSuccessStatusCode.Should().BeTrue(); // 1h without a project

        const string range = "from=2026-03-01&to=2026-03-31";
        var perProject = await SendOkAsync<List<ProjectHoursLineDto>>(_client, HttpMethod.Get, $"/api/reports/projects?{range}", token);
        perProject.Select(l => (l.ProjectName, l.ClientName, l.IsBillable, l.TotalHours)).Should().Equal(
            ("Internal", null, false, 2d),
            ("Website", "Acme", true, 4d),
            (null, null, false, 1d));

        var csv = await (await SendAsync(_client, HttpMethod.Get, $"/api/reports/projects/csv?{range}", token)).Content.ReadAsStringAsync();
        csv.Should().Contain("Project,Client,Billable,Hours,Entries").And.Contain("Website,Acme,yes,4,1").And.Contain(",,,1,1");

        var report = await SendOkAsync<HoursReportDto>(_client, HttpMethod.Get, $"/api/reports/hours?{range}&projectId={website}", token);
        report.GrandTotalHours.Should().Be(4);

        var entries = await (await SendAsync(_client, HttpMethod.Get, $"/api/reports/entries/csv?{range}&projectId={internalWork}", token))
            .Content.ReadAsStringAsync();
        entries.Trim().Split('\n').Should().HaveCount(2).And.Contain(l => l.Contains("Internal"));
    }
}

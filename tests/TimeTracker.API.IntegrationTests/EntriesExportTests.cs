using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using TimeTracker.API.Controllers;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.Users.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>The one-row-per-entry CSV export, and the public server info a phone app checks.</summary>
public class EntriesExportTests : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client;

    public EntriesExportTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EntriesCsv_HasOneRowPerEntry_WithTimesProjectAndEscapedText() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var email = $"{Guid.NewGuid()}@employee.local";
        var employee = await SendOkAsync<UserDto>(_client, HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = "=Eva", LastName = "Smith, Jr", Email = email, Password, Role = "Employee"
        });
        var employeeToken = await LoginAsync(_client, email, Password);
        var work = (await SendOkAsync<List<HourTypeDto>>(_client, HttpMethod.Get, "/api/hourtypes", employeeToken))
            .First(t => !t.IsLeave);
        var projectId = await SendOkAsync<Guid>(_client, HttpMethod.Post, "/api/projects", employerToken,
            new { Name = "Website", ClientName = "Acme" });

        var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        foreach (var (start, end, breakMinutes) in new[] { ("13:00", "17:00", 0), ("08:00", "12:30", 30) }) {
            (await SendAsync(_client, HttpMethod.Post, "/api/timeentries", employeeToken, new {
                HourTypeId = work.Id, ProjectId = projectId, WorkDate = day.ToString("yyyy-MM-dd"),
                StartTime = start, EndTime = end, BreakMinutes = breakMinutes,
                Notes = "Client visit, then \"office\"", Location = "Gent"
            })).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var query = $"from={day:yyyy-MM-dd}&to={day:yyyy-MM-dd}";
        var response = await SendAsync(_client, HttpMethod.Get, $"/api/reports/entries/csv?{query}", employerToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var rows = (await response.Content.ReadAsStringAsync()).TrimEnd().Split('\n').Select(r => r.TrimEnd('\r')).ToList();
        rows[0].Should().Be("Employee,Date,Start,End,BreakMinutes,Hours,HourType,Project,Location,Notes,RecordedAtUtc,LastChangedAtUtc");
        rows.Should().HaveCount(3);

        // Sorted by start time; names can't turn into formulas; text with commas and quotes stays in one cell.
        rows[1].Should().StartWith($"\"'=Eva Smith, Jr\",{day:yyyy-MM-dd},08:00,12:30,30,4,");
        rows[2].Should().StartWith($"\"'=Eva Smith, Jr\",{day:yyyy-MM-dd},13:00,17:00,0,4,");
        rows[1].Should().Contain(",Website,Gent,\"Client visit, then \"\"office\"\"\",");
        rows[1].Should().MatchRegex(@",\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z,(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)?$");

        // An employee may export their own entries, but not someone else's.
        (await SendAsync(_client, HttpMethod.Get, $"/api/reports/entries/csv?{query}&userId={employee.Id}", employeeToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var employerId = (await SendOkAsync<List<UserDto>>(_client, HttpMethod.Get, "/api/users", employerToken))
            .Single(u => u.Id != employee.Id).Id;
        (await SendAsync(_client, HttpMethod.Get, $"/api/reports/entries/csv?{query}&userId={employerId}", employeeToken))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Not without signing in.
        (await _client.GetAsync($"/api/reports/entries/csv?{query}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Info_IsPublic_AndNamesTheServerAndApiVersion() {
        var response = await _client.GetAsync("/api/info");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var info = (await response.Content.ReadFromJsonAsync<ServerInfoDto>())!;
        info.Name.Should().Be("TimeTracker");
        info.ApiVersion.Should().Be(InfoController.ApiVersion);
    }
}

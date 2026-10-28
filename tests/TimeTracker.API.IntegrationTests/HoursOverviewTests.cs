using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.HoursOverview;
using TimeTracker.Application.HoursOverview.Dtos;
using TimeTracker.Application.Leave;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Application.Users.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>Worked versus scheduled hours, for yourself and for the people you manage.</summary>
public class HoursOverviewTests : IClassFixture<CustomWebApplicationFactory> {
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public HoursOverviewTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private async Task<T> SendOkAsync<T>(HttpMethod method, string url, string token, object? body = null) {
        using var request = Authorized(method, url, token);
        if (body is not null) {
            request.Content = JsonContent.Create(body, options: Json);
        }

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private Task<T> GetAsync<T>(string url, string token) => SendOkAsync<T>(HttpMethod.Get, url, token);

    private async Task<(UserDto User, string Token)> CreateEmployeeAsync(string employerToken, string firstName) {
        var email = $"{Guid.NewGuid()}@employee.local";
        var user = await SendOkAsync<UserDto>(HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = firstName, LastName = "Test", Email = email, Password, Role = "Employee"
        });
        return (user, await LoginAsync(_client, email, Password));
    }

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd");

    [Fact]
    public async Task ComparesLoggedHoursWithTheScheduleForYourselfAndYourTeam() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (lead, leadToken) = await CreateEmployeeAsync(employerToken, "Lead");
        var (member, memberToken) = await CreateEmployeeAsync(employerToken, "Member");
        using (var assign = Authorized(HttpMethod.Post, $"/api/users/{member.Id}/supervisor", employerToken)) {
            assign.Content = JsonContent.Create(lead.Id);
            (await _client.SendAsync(assign)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // Company hours: Monday to Thursday 8h, Friday 6.5h.
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }
            .Select(d => new { DayOfWeek = d, StartTime = "08:30", EndTime = "17:30", BreakMinutes = 60 })
            .Append(new { DayOfWeek = DayOfWeek.Friday, StartTime = "08:30", EndTime = "15:30", BreakMinutes = 30 })
            .ToArray();
        await SendOkAsync<object>(HttpMethod.Put, "/api/work-schedules/company", employerToken, days);

        // Next week, so everything is after the accounts were created.
        var monday = HoursOverviewCalculator.StartOfWeek(DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(7);
        var wednesday = monday.AddDays(2);
        var friday = monday.AddDays(4);

        var workType = (await GetAsync<List<HourTypeDto>>("/api/hourtypes", memberToken)).Single(t => t.IsDefault);
        var paidLeave = (await GetAsync<List<HourTypeDto>>("/api/hourtypes", memberToken)).Single(t => t.DeductsFromLeaveBalance);
        foreach (var (date, start, end) in new[] { (monday, "08:00", "18:00"), (monday.AddDays(1), "08:30", "17:30") }) {
            await SendOkAsync<object>(HttpMethod.Post, "/api/timeentries", memberToken, new {
                HourTypeId = workType.Id, WorkDate = Iso(date), StartTime = start, EndTime = end, BreakMinutes = 60
            });
        }

        // Friday is approved leave.
        var leave = await SendOkAsync<LeaveRequestDto>(HttpMethod.Post, "/api/leave-requests", memberToken, new {
            HourTypeId = paidLeave.Id, StartDate = Iso(friday), EndDate = Iso(friday)
        });
        await SendOkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{leave.Id}/approve", leadToken);

        // Wednesday, nothing logged yet that day: Monday and Tuesday count (16h expected, 17h logged).
        var mine = await GetAsync<MyHoursOverviewDto>($"/api/hours-overview/me?asOf={Iso(wednesday)}", memberToken);
        mine.ScheduleSource.Should().Be(WorkScheduleSource.Company);
        mine.WeeklyScheduleHours.Should().Be(38.5);
        mine.Week.ExpectedHours.Should().Be(16);
        mine.Week.WorkedHours.Should().Be(17);
        mine.Week.DifferenceHours.Should().Be(1);
        mine.Days.Should().HaveCount(7);
        mine.Days[2].IsPending.Should().BeTrue("today isn't logged yet");
        mine.Days[4].Should().BeEquivalentTo(new { Date = friday, HasLeave = true, IsPending = true, WorkedHours = 6.5m });

        // On Friday the leave counts as worked.
        (await GetAsync<MyHoursOverviewDto>($"/api/hours-overview/me?asOf={Iso(friday)}", memberToken))
            .Week.Should().BeEquivalentTo(new { ExpectedHours = 38.5m, WorkedHours = 23.5m, DifferenceHours = -15m });

        // Employers see everyone, the supervisor only their team, others nobody.
        var all = await GetAsync<List<TeamMemberHoursDto>>($"/api/hours-overview/team?asOf={Iso(wednesday)}", employerToken);
        all.Should().HaveCount(3);
        all.Single(r => r.UserId == member.Id).Week.DifferenceHours.Should().Be(1);

        (await GetAsync<List<TeamMemberHoursDto>>($"/api/hours-overview/team?asOf={Iso(wednesday)}", leadToken))
            .Should().ContainSingle().Which.UserId.Should().Be(member.Id);
        (await GetAsync<List<TeamMemberHoursDto>>($"/api/hours-overview/team?asOf={Iso(wednesday)}", memberToken))
            .Should().BeEmpty();

        // Another company sees none of these people.
        var other = await CreateTenantAsync(_client);
        var otherToken = await LoginAsync(_client, other.EmployerEmail, Password);
        (await GetAsync<List<TeamMemberHoursDto>>("/api/hours-overview/team", otherToken))
            .Should().ContainSingle().Which.FullName.Should().NotBe("Member Test");
    }

    [Fact]
    public async Task PeopleWhoDontTrackHoursAreLeftOut() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (employee, employeeToken) = await CreateEmployeeAsync(employerToken, "Worker");
        var employer = (await GetAsync<List<UserDto>>("/api/users", employerToken)).Single(u => u.Role == "Employer");
        employer.TracksHours.Should().BeTrue("everyone tracks hours by default");

        // Employees can't switch it, not even for themselves.
        using (var forbidden = Authorized(HttpMethod.Put, $"/api/users/{employee.Id}/tracks-hours", employeeToken)) {
            forbidden.Content = JsonContent.Create(new { TracksHours = false });
            (await _client.SendAsync(forbidden)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // The owner doesn't log hours.
        using (var request = Authorized(HttpMethod.Put, $"/api/users/{employer.Id}/tracks-hours", employerToken)) {
            request.Content = JsonContent.Create(new { TracksHours = false });
            (await _client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        (await GetAsync<List<UserDto>>("/api/users", employerToken))
            .Single(u => u.Id == employer.Id).TracksHours.Should().BeFalse();
        (await GetAsync<List<TeamMemberHoursDto>>("/api/hours-overview/team", employerToken))
            .Should().ContainSingle().Which.UserId.Should().Be(employee.Id);
        (await GetAsync<MyHoursOverviewDto>("/api/hours-overview/me", employerToken)).TracksHours.Should().BeFalse();
        (await GetAsync<MyHoursOverviewDto>("/api/hours-overview/me", employeeToken)).TracksHours.Should().BeTrue();
    }
}

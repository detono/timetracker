using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Application.LockedMonths;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Application.Users.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>
/// A locked month's hours can't change, for anyone, until an employer unlocks it. That covers
/// time entries and leave bookings; rejecting or withdrawing a pending request still works.
/// </summary>
public class MonthLockTests : IClassFixture<CustomWebApplicationFactory> {
    // September 2025 lies in the past whenever these tests run.
    private const int Year = 2025;
    private const int Month = 9;
    private const string InSeptember = "2025-09-10";
    private const string InOctober = "2025-10-08";
    private const string LockUrl = "/api/locked-months/2025/9";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public MonthLockTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) {
        using var request = Authorized(method, url, token);
        if (body is not null) {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return await _client.SendAsync(request);
    }

    private async Task<T> SendOkAsync<T>(HttpMethod method, string url, string token, object? body = null) {
        var response = await SendAsync(method, url, token, body);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<(string EmployerToken, string EmployeeToken)> SetUpAsync() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var email = $"{Guid.NewGuid()}@employee.local";
        await SendOkAsync<UserDto>(HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = "Eve", LastName = "Employee", Email = email, Password, Role = "Employee"
        });
        return (employerToken, await LoginAsync(_client, email, Password));
    }

    private async Task<HourTypeDto> HourTypeAsync(string token, bool leave) =>
        (await SendOkAsync<List<HourTypeDto>>(HttpMethod.Get, "/api/hourtypes", token))
        .First(t => leave ? t.DeductsFromLeaveBalance : !t.IsLeave);

    private static object Entry(Guid hourTypeId, string date, Guid? id = null) => new {
        Id = id, HourTypeId = hourTypeId, WorkDate = date, StartTime = "09:00", EndTime = "17:00", BreakMinutes = 30
    };

    [Fact]
    public async Task OnlyEmployersLockStartedMonths_AndEveryoneSeesTheLocks() {
        var (employerToken, employeeToken) = await SetUpAsync();

        (await SendAsync(HttpMethod.Put, LockUrl, employeeToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Put, "/api/locked-months/2099/1", employerToken)).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);

        var locked = await SendOkAsync<LockedMonthDto>(HttpMethod.Put, LockUrl, employerToken);
        locked.Should().Match<LockedMonthDto>(m => m.Year == Year && m.Month == Month && m.LockedByName == "Test Employer");
        // Locking twice changes nothing.
        await SendOkAsync<LockedMonthDto>(HttpMethod.Put, LockUrl, employerToken);

        (await SendOkAsync<List<LockedMonthDto>>(HttpMethod.Get, "/api/locked-months", employeeToken))
            .Should().ContainSingle(m => m.Year == Year && m.Month == Month);

        (await SendAsync(HttpMethod.Delete, LockUrl, employeeToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Delete, LockUrl, employerToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendOkAsync<List<LockedMonthDto>>(HttpMethod.Get, "/api/locked-months", employeeToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task HoursInALockedMonth_CantBeAddedChangedMovedOrDeleted_EvenByAnEmployer() {
        var (employerToken, employeeToken) = await SetUpAsync();
        var work = await HourTypeAsync(employeeToken, leave: false);
        var september = await SendOkAsync<TimeEntryDto>(HttpMethod.Post, "/api/timeentries", employeeToken, Entry(work.Id, InSeptember));
        var october = await SendOkAsync<TimeEntryDto>(HttpMethod.Post, "/api/timeentries", employeeToken, Entry(work.Id, InOctober));

        await SendOkAsync<LockedMonthDto>(HttpMethod.Put, LockUrl, employerToken);

        foreach (var token in new[] { employeeToken, employerToken }) {
            (await SendAsync(HttpMethod.Post, "/api/timeentries", token, Entry(work.Id, "2025-09-11"))).StatusCode
                .Should().Be(HttpStatusCode.Conflict);
            (await SendAsync(HttpMethod.Put, $"/api/timeentries/{september.Id}", token, Entry(work.Id, InSeptember, september.Id)))
                .StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await SendAsync(HttpMethod.Put, $"/api/timeentries/{september.Id}", token, Entry(work.Id, InOctober, september.Id)))
                .StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await SendAsync(HttpMethod.Put, $"/api/timeentries/{october.Id}", token, Entry(work.Id, InSeptember, october.Id)))
                .StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await SendAsync(HttpMethod.Delete, $"/api/timeentries/{september.Id}", token)).StatusCode
                .Should().Be(HttpStatusCode.Conflict);
        }

        var message = await (await SendAsync(HttpMethod.Delete, $"/api/timeentries/{september.Id}", employeeToken))
            .Content.ReadAsStringAsync();
        message.Should().Contain("September 2025 is locked");

        // October isn't locked.
        await SendOkAsync<TimeEntryDto>(HttpMethod.Put, $"/api/timeentries/{october.Id}", employeeToken,
            Entry(work.Id, "2025-10-09", october.Id));

        await SendAsync(HttpMethod.Delete, LockUrl, employerToken);
        (await SendAsync(HttpMethod.Delete, $"/api/timeentries/{september.Id}", employeeToken)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LeaveInALockedMonth_CantBeRequestedApprovedOrRevoked_ButCanBeRejected() {
        var (employerToken, employeeToken) = await SetUpAsync();
        var paidLeave = await HourTypeAsync(employeeToken, leave: true);

        // Tuesday 30 Sep to Wednesday 1 Oct 2025: partly in September.
        var toReject = await SendOkAsync<LeaveRequestDto>(HttpMethod.Post, "/api/leave-requests", employeeToken, new {
            HourTypeId = paidLeave.Id, StartDate = "2025-09-30", EndDate = "2025-10-01"
        });
        var toRevoke = await SendOkAsync<LeaveRequestDto>(HttpMethod.Post, "/api/leave-requests", employeeToken, new {
            HourTypeId = paidLeave.Id, StartDate = "2025-09-15", EndDate = "2025-09-15"
        });
        await SendOkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{toRevoke.Id}/approve", employerToken);

        await SendOkAsync<LockedMonthDto>(HttpMethod.Put, LockUrl, employerToken);

        (await SendAsync(HttpMethod.Post, "/api/leave-requests", employeeToken, new {
            HourTypeId = paidLeave.Id, StartDate = "2025-09-22", EndDate = "2025-09-22"
        })).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SendAsync(HttpMethod.Post, $"/api/leave-requests/{toReject.Id}/approve", employerToken)).StatusCode
            .Should().Be(HttpStatusCode.Conflict);
        (await SendAsync(HttpMethod.Post, $"/api/leave-requests/{toRevoke.Id}/cancel", employerToken)).StatusCode
            .Should().Be(HttpStatusCode.Conflict);

        var list = await SendOkAsync<List<LeaveRequestDto>>(HttpMethod.Get, "/api/leave-requests?scope=ToReview", employerToken);
        list.Should().OnlyContain(r => r.InLockedMonth);
        list.Single(r => r.Id == toRevoke.Id).CanCancel.Should().BeFalse();

        var rejected = await SendOkAsync<LeaveRequestDto>(
            HttpMethod.Post, $"/api/leave-requests/{toReject.Id}/reject", employerToken);
        rejected.Status.Should().Be("Rejected");
    }
}

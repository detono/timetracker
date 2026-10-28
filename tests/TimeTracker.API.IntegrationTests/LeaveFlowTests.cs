using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.Leave;
using TimeTracker.Application.LeaveBalances.Dtos;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Application.Users.Dtos;
using TimeTracker.Application.WorkSchedules.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>
/// Leave from request to booked hours: work schedules decide the hours, approval books
/// them as time entries, the balance warns without blocking, and cancelling undoes it.
/// </summary>
public class LeaveFlowTests : IClassFixture<CustomWebApplicationFactory> {
    // Monday 2 Nov 2026 to Sunday 8 Nov 2026.
    private const string Monday = "2026-11-02";
    private const string Sunday = "2026-11-08";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public LeaveFlowTests(CustomWebApplicationFactory factory) {
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

    private Task<T> GetAsync<T>(string url, string token) => SendOkAsync<T>(HttpMethod.Get, url, token);

    private async Task<(UserDto User, string Token)> CreateEmployeeAsync(string employerToken) {
        var email = $"{Guid.NewGuid()}@employee.local";
        var user = await SendOkAsync<UserDto>(HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = "Eve", LastName = "Employee", Email = email, Password, Role = "Employee"
        });
        return (user, await LoginAsync(_client, email, Password));
    }

    private static object FourDayWeek => new[] {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday
    }.Select(d => new { DayOfWeek = d, StartTime = "08:00", EndTime = "16:30", BreakMinutes = 30 }).ToArray();

    private async Task<HourTypeDto> PaidLeaveAsync(string token) =>
        (await GetAsync<List<HourTypeDto>>("/api/hourtypes", token)).Single(t => t.DeductsFromLeaveBalance);

    [Fact]
    public async Task ApprovedLeaveIsBookedOnScheduledDaysAndCountsAgainstTheBalance() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (employee, employeeToken) = await CreateEmployeeAsync(employerToken);
        var paidLeave = await PaidLeaveAsync(employeeToken);

        // Starter types: "Paid time off" deducts, "Sick leave" can be requested but doesn't.
        var types = await GetAsync<List<HourTypeDto>>("/api/hourtypes", employeeToken);
        types.Count(t => t.IsLeave).Should().Be(2);

        // Without a saved schedule, the default applies.
        var defaultSchedule = await GetAsync<WorkScheduleDto>($"/api/work-schedules/{employee.Id}", employeeToken);
        defaultSchedule.Source.Should().Be(WorkScheduleSource.BuiltIn);
        defaultSchedule.WeeklyHours.Should().Be(37.5);

        // Employees can't change their own schedule or allowance.
        (await SendAsync(HttpMethod.Put, $"/api/work-schedules/{employee.Id}", employeeToken, FourDayWeek))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Put, $"/api/leave-balances/{employee.Id}/2026", employeeToken, new { AllowanceHours = 999 }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The employer sets a four-day week of 8 hours, and 16 hours of leave for the year.
        var schedule = await SendOkAsync<WorkScheduleDto>(
            HttpMethod.Put, $"/api/work-schedules/{employee.Id}", employerToken, FourDayWeek);
        schedule.WeeklyHours.Should().Be(32);
        (await SendAsync(HttpMethod.Put, $"/api/leave-balances/{employee.Id}/2026", employerToken, new { AllowanceHours = 16 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // A full calendar week is 4 working days = 32 hours, which is more than the 16 left.
        var preview = await GetAsync<LeavePreviewDto>(
            $"/api/leave-requests/preview?hourTypeId={paidLeave.Id}&startDate={Monday}&endDate={Sunday}", employeeToken);
        preview.WorkingDays.Should().Be(4);
        preview.Hours.Should().Be(32);
        preview.RemainingBalanceHours.Should().Be(16);
        preview.ExceedsBalance.Should().BeTrue();

        var created = await SendOkAsync<LeaveRequestDto>(HttpMethod.Post, "/api/leave-requests", employeeToken, new {
            HourTypeId = paidLeave.Id, StartDate = Monday, EndDate = Sunday, Comment = "Family trip"
        });
        created.Status.Should().Be("Pending");
        created.CanReview.Should().BeFalse();
        created.CanCancel.Should().BeTrue();

        // Overlapping requests are refused.
        (await SendAsync(HttpMethod.Post, "/api/leave-requests", employeeToken, new {
            HourTypeId = paidLeave.Id, StartDate = Sunday, EndDate = Sunday
        })).StatusCode.Should().Be(HttpStatusCode.Conflict);

        // The employer is notified in-app; the employee reviews nobody and can't approve their own leave.
        (await GetAsync<int>("/api/leave-requests/pending-count", employerToken)).Should().Be(1);
        (await GetAsync<int>("/api/leave-requests/pending-count", employeeToken)).Should().Be(0);
        (await SendAsync(HttpMethod.Post, $"/api/leave-requests/{created.Id}/approve", employeeToken))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var toReview = await GetAsync<List<LeaveRequestDto>>("/api/leave-requests?scope=ToReview&status=Pending", employerToken);
        toReview.Should().ContainSingle().Which.ExceedsBalance.Should().BeTrue("the warning is shown to the approver");

        // Over the balance is a warning, not a block.
        var approved = await SendOkAsync<LeaveRequestDto>(
            HttpMethod.Post, $"/api/leave-requests/{created.Id}/approve", employerToken, new { Comment = "Have fun" });
        approved.Status.Should().Be("Approved");
        approved.ReviewComment.Should().Be("Have fun");
        (await GetAsync<int>("/api/leave-requests/pending-count", employerToken)).Should().Be(0);

        var entries = await GetAsync<List<TimeEntryDto>>(
            $"/api/timeentries?from={Monday}&to={Sunday}", employeeToken);
        entries.Should().HaveCount(4);
        entries.Should().OnlyContain(e => e.HourTypeId == paidLeave.Id && e.DurationHours == 8);
        entries.Select(e => e.WorkDate).Should().NotContain(new DateOnly(2026, 11, 6), "Friday is a day off");

        var balance = (await GetAsync<List<LeaveBalanceDto>>("/api/leave-balances?year=2026", employerToken))
            .Single(b => b.UserId == employee.Id);
        balance.UsedHours.Should().Be(32);
        balance.RemainingHours.Should().Be(-16);

        // Approved leave can't be withdrawn by the employee, only revoked by a reviewer.
        (await SendAsync(HttpMethod.Post, $"/api/leave-requests/{created.Id}/cancel", employeeToken))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var cancelled = await SendOkAsync<LeaveRequestDto>(
            HttpMethod.Post, $"/api/leave-requests/{created.Id}/cancel", employerToken);
        cancelled.Status.Should().Be("Cancelled");

        (await GetAsync<List<TimeEntryDto>>($"/api/timeentries?from={Monday}&to={Sunday}", employeeToken))
            .Should().BeEmpty("revoking leave removes the hours it booked");
        (await GetAsync<List<LeaveBalanceDto>>("/api/leave-balances?year=2026", employeeToken))
            .Should().ContainSingle().Which.RemainingHours.Should().Be(16);
    }

    [Fact]
    public async Task EmployeesFollowTheCompanyScheduleUnlessTheyHaveTheirOwn() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (employee, employeeToken) = await CreateEmployeeAsync(employerToken);
        var paidLeave = await PaidLeaveAsync(employeeToken);

        // Company hours: Monday to Thursday 8:30-17:30, Friday 8:30-15:30, 30-minute break (8.5h and 6.5h).
        var companyDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }
            .Select(d => new { DayOfWeek = d, StartTime = "08:30", EndTime = "17:30", BreakMinutes = 30 })
            .Append(new { DayOfWeek = DayOfWeek.Friday, StartTime = "08:30", EndTime = "15:30", BreakMinutes = 30 })
            .ToArray();
        (await SendAsync(HttpMethod.Put, "/api/work-schedules/company", employeeToken, companyDays))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var company = await SendOkAsync<WorkScheduleDto>(HttpMethod.Put, "/api/work-schedules/company", employerToken, companyDays);
        company.Source.Should().Be(WorkScheduleSource.Company);
        company.WeeklyHours.Should().Be(40.5);

        // Without their own schedule, the employee follows the company's: a Friday off is 6.5 hours.
        var followed = await GetAsync<WorkScheduleDto>($"/api/work-schedules/{employee.Id}", employeeToken);
        followed.Source.Should().Be(WorkScheduleSource.Company);
        followed.WeeklyHours.Should().Be(40.5);
        var friday = await GetAsync<LeavePreviewDto>(
            $"/api/leave-requests/preview?hourTypeId={paidLeave.Id}&startDate=2026-11-06&endDate=2026-11-06", employeeToken);
        friday.Hours.Should().Be(6.5m);

        // Their own schedule wins; resetting puts them back on the company's.
        var own = await SendOkAsync<WorkScheduleDto>(
            HttpMethod.Put, $"/api/work-schedules/{employee.Id}", employerToken, FourDayWeek);
        own.Source.Should().Be(WorkScheduleSource.Personal);
        (await GetAsync<LeavePreviewDto>(
            $"/api/leave-requests/preview?hourTypeId={paidLeave.Id}&startDate=2026-11-06&endDate=2026-11-06", employeeToken))
            .WorkingDays.Should().Be(0, "Friday is a day off in their own schedule");

        var reset = await SendOkAsync<WorkScheduleDto>(HttpMethod.Delete, $"/api/work-schedules/{employee.Id}", employerToken);
        reset.Source.Should().Be(WorkScheduleSource.Company);
        (await GetAsync<WorkScheduleDto>($"/api/work-schedules/{employee.Id}", employeeToken))
            .WeeklyHours.Should().Be(40.5);

        // Another company still gets the built-in default.
        var other = await CreateTenantAsync(_client);
        var otherToken = await LoginAsync(_client, other.EmployerEmail, Password);
        (await GetAsync<WorkScheduleDto>("/api/work-schedules/company", otherToken))
            .Source.Should().Be(WorkScheduleSource.BuiltIn);
    }

    [Fact]
    public async Task SupervisorsReviewTheirTeamOnlyAndOtherCompaniesSeeNothing() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (lead, leadToken) = await CreateEmployeeAsync(employerToken);
        var (member, memberToken) = await CreateEmployeeAsync(employerToken);
        var (_, outsiderToken) = await CreateEmployeeAsync(employerToken);

        (await SendAsync(HttpMethod.Post, $"/api/users/{member.Id}/supervisor", employerToken, lead.Id))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var paidLeave = await PaidLeaveAsync(memberToken);
        var request = await SendOkAsync<LeaveRequestDto>(HttpMethod.Post, "/api/leave-requests", memberToken, new {
            HourTypeId = paidLeave.Id, StartDate = Monday, EndDate = Monday
        });
        request.Hours.Should().Be(7.5m);

        // The supervisor sees and can review it; a colleague can't even see it.
        (await GetAsync<int>("/api/leave-requests/pending-count", leadToken)).Should().Be(1);
        (await GetAsync<List<LeaveRequestDto>>("/api/leave-requests?scope=ToReview", leadToken))
            .Should().ContainSingle().Which.CanReview.Should().BeTrue();
        (await GetAsync<List<LeaveRequestDto>>("/api/leave-requests?scope=ToReview", outsiderToken)).Should().BeEmpty();
        (await SendAsync(HttpMethod.Post, $"/api/leave-requests/{request.Id}/reject", outsiderToken))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Post, $"/api/leave-requests/{request.Id}/cancel", outsiderToken))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Another company can't see or touch it.
        var otherTenant = await CreateTenantAsync(_client);
        var otherToken = await LoginAsync(_client, otherTenant.EmployerEmail, Password);
        (await GetAsync<List<LeaveRequestDto>>("/api/leave-requests?scope=ToReview", otherToken)).Should().BeEmpty();
        (await SendAsync(HttpMethod.Post, $"/api/leave-requests/{request.Id}/approve", otherToken))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(HttpMethod.Get, $"/api/work-schedules/{member.Id}", otherToken))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        var rejected = await SendOkAsync<LeaveRequestDto>(
            HttpMethod.Post, $"/api/leave-requests/{request.Id}/reject", leadToken, new { Comment = "Release week" });
        rejected.Status.Should().Be("Rejected");
        rejected.ReviewedByName.Should().Be("Eve Employee");

        var mine = await GetAsync<List<LeaveRequestDto>>("/api/leave-requests?scope=Mine", memberToken);
        mine.Should().ContainSingle().Which.ReviewComment.Should().Be("Release week");
        (await GetAsync<List<TimeEntryDto>>($"/api/timeentries?from={Monday}&to={Monday}", memberToken)).Should().BeEmpty();
    }
}

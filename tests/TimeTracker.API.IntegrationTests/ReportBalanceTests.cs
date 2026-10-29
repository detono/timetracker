using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.Reports.Queries.GetHoursBalance;
using TimeTracker.Application.Users.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>Worked vs. expected in reports and their CSV, and that CSV cells can't become formulas.</summary>
public class ReportBalanceTests : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client;

    public ReportBalanceTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) {
        using var request = Authorized(method, url, token);
        if (body is not null) {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private async Task<T> GetAsync<T>(string url, string token) {
        var response = await SendAsync(HttpMethod.Get, url, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    [Fact]
    public async Task Balance_ComparesLoggedHoursWithTheSchedule_FromTheFirstLoggedDay() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var email = $"{Guid.NewGuid()}@employee.local";
        var create = await SendAsync(HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = "=Eva", LastName = "Smith, Jr", Email = email, Password, Role = "Employee"
        });
        var employee = (await create.Content.ReadFromJsonAsync<UserDto>())!;
        var employeeToken = await LoginAsync(_client, email, Password);
        var work = (await GetAsync<List<HourTypeDto>>("/api/hourtypes", employeeToken)).First(t => !t.IsLeave);

        // Last week, Monday to Sunday: 8 hours on Monday and on Tuesday.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7) - 7);
        var sunday = monday.AddDays(6);
        foreach (var day in new[] { monday, monday.AddDays(1) }) {
            (await SendAsync(HttpMethod.Post, "/api/timeentries", employeeToken, new {
                HourTypeId = work.Id, WorkDate = day.ToString("yyyy-MM-dd"), StartTime = "09:00", EndTime = "17:00", BreakMinutes = 0
            })).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var query = $"from={monday:yyyy-MM-dd}&to={sunday:yyyy-MM-dd}&grouping=Week";
        var lines = await GetAsync<List<HoursBalanceLineDto>>($"/api/reports/hours/balance?{query}", employerToken);

        // Built-in schedule: Monday to Friday, 7.5 hours a day.
        var line = lines.Single(l => l.UserId == employee.Id);
        line.WorkedHours.Should().Be(16);
        line.ExpectedHours.Should().Be(37.5m);
        line.DifferenceHours.Should().Be(-21.5m);

        // An employee only sees themself.
        (await GetAsync<List<HoursBalanceLineDto>>($"/api/reports/hours/balance?{query}", employeeToken))
            .Should().OnlyContain(l => l.UserId == employee.Id);

        var csv = await (await SendAsync(HttpMethod.Get, $"/api/reports/hours/balance/csv?{query}", employerToken))
            .Content.ReadAsStringAsync();
        csv.Should().StartWith("Employee,Period,PeriodStart,PeriodEnd,WorkedHours,ExpectedHours,DifferenceHours");
        csv.Should().Contain($"\"'=Eva Smith, Jr\",{line.PeriodLabel},{monday:yyyy-MM-dd},{sunday:yyyy-MM-dd},16,37.5,-21.5");

        // The hours CSV escapes names the same way.
        var hoursCsv = await (await SendAsync(HttpMethod.Get, $"/api/reports/hours/csv?{query}", employerToken))
            .Content.ReadAsStringAsync();
        hoursCsv.Should().Contain("\"'=Eva Smith, Jr\"");
    }
}

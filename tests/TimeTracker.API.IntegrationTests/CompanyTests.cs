using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TimeTracker.Application.Company.Dtos;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.HoursOverview.Dtos;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Application.PublicHolidays.Dtos;
using TimeTracker.Application.Users.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>Company details, public holidays, and how holidays affect leave and the hours overview.</summary>
public class CompanyTests : IClassFixture<CustomWebApplicationFactory> {
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public CompanyTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) {
        using var request = Authorized(method, url, token);
        if (body is not null) {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return await _client.SendAsync(request);
    }

    private async Task<T> OkAsync<T>(HttpMethod method, string url, string token, object? body = null) {
        var response = await SendAsync(method, url, token, body);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<(UserDto User, string Token)> CreateEmployeeAsync(string employerToken) {
        var email = $"{Guid.NewGuid()}@employee.local";
        var user = await OkAsync<UserDto>(HttpMethod.Post, "/api/users", employerToken, new {
            FirstName = "Eve", LastName = "Employee", Email = email, Password, Role = "Employee"
        });
        return (user, await LoginAsync(_client, email, Password));
    }

    [Fact]
    public async Task EmployersManageCompanyDetailsAndHolidays() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (_, employeeToken) = await CreateEmployeeAsync(employerToken);

        // Importing needs a country.
        (await SendAsync(HttpMethod.Post, "/api/public-holidays/import?year=2026", employerToken))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var newName = $"Acme {Guid.NewGuid()}";
        var company = await OkAsync<CompanyDto>(HttpMethod.Put, "/api/company", employerToken, new {
            Name = newName, Street = "Kerkstraat 1", PostalCode = "9000", City = "Gent", CountryCode = "be", Region = (string?)null
        });
        company.Should().BeEquivalentTo(new { Name = newName, City = "Gent", CountryCode = "BE" });

        // Employees can't see or change the company details, or manage holidays.
        (await SendAsync(HttpMethod.Get, "/api/company", employeeToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Post, "/api/public-holidays/import?year=2026", employeeToken))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Another company can't take the same name.
        var other = await CreateTenantAsync(_client);
        var otherToken = await LoginAsync(_client, other.EmployerEmail, Password);
        (await SendAsync(HttpMethod.Put, "/api/company", otherToken, new { Name = newName }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Import: nationwide holidays only (no region set); running it again adds nothing.
        var import = await OkAsync<PublicHolidayImportDto>(HttpMethod.Post, "/api/public-holidays/import?year=2026", employerToken);
        import.Added.Should().Be(2);
        import.Holidays.Select(h => h.Name).Should().Equal("Allerheiligen", "Wapenstilstand");
        (await OkAsync<PublicHolidayImportDto>(HttpMethod.Post, "/api/public-holidays/import?year=2026", employerToken))
            .Added.Should().Be(0);

        // Own days off can be added and removed; everyone can see the list, other companies can't.
        var dayOff = await OkAsync<PublicHolidayDto>(HttpMethod.Post, "/api/public-holidays", employerToken,
            new { Date = "2026-12-24", Name = "Company day off" });
        (await SendAsync(HttpMethod.Post, "/api/public-holidays", employerToken, new { Date = "2026-12-24", Name = "Again" }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await OkAsync<List<PublicHolidayDto>>(HttpMethod.Get, "/api/public-holidays?year=2026", employeeToken)).Should().HaveCount(3);
        (await OkAsync<List<PublicHolidayDto>>(HttpMethod.Get, "/api/public-holidays?year=2026", otherToken)).Should().BeEmpty();
        (await SendAsync(HttpMethod.Delete, $"/api/public-holidays/{dayOff.Id}", otherToken))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(HttpMethod.Delete, $"/api/public-holidays/{dayOff.Id}", employerToken))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RegionalHolidaysOnlyApplyToTheirRegion() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        await OkAsync<CompanyDto>(HttpMethod.Put, "/api/company", token,
            new { Name = $"Bayern {Guid.NewGuid()}", CountryCode = "DE", Region = "DE-BY" });

        var import = await OkAsync<PublicHolidayImportDto>(HttpMethod.Post, "/api/public-holidays/import?year=2026", token);

        import.Holidays.Select(h => h.Name).Should().Contain("Heilige Drei Könige");

        await OkAsync<CompanyDto>(HttpMethod.Put, "/api/company", token, new { Name = $"Unknown {Guid.NewGuid()}", CountryCode = "XX" });
        var failed = await SendAsync(HttpMethod.Post, "/api/public-holidays/import?year=2027", token);
        failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await failed.Content.ReadAsStringAsync()).Should().Contain("by hand");
    }

    [Fact]
    public async Task HolidaysAreSkippedInLeaveAndTheHoursOverview_AndEmployersDontRequestLeave() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (employee, employeeToken) = await CreateEmployeeAsync(employerToken);
        await OkAsync<CompanyDto>(HttpMethod.Put, "/api/company", employerToken, new { Name = $"Gent {Guid.NewGuid()}", CountryCode = "BE" });
        await OkAsync<PublicHolidayImportDto>(HttpMethod.Post, "/api/public-holidays/import?year=2026", employerToken);

        var paidLeave = (await OkAsync<List<HourTypeDto>>(HttpMethod.Get, "/api/hourtypes", employeeToken))
            .Single(t => t.DeductsFromLeaveBalance);

        // Employers log their own time off as hours instead.
        (await SendAsync(HttpMethod.Post, "/api/leave-requests", employerToken, new {
            HourTypeId = paidLeave.Id, StartDate = "2026-11-09", EndDate = "2026-11-13"
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Mon 9 - Fri 13 Nov with Wed 11 Nov a holiday: 4 days of 7.5h.
        var request = await OkAsync<LeaveRequestDto>(HttpMethod.Post, "/api/leave-requests", employeeToken, new {
            HourTypeId = paidLeave.Id, StartDate = "2026-11-09", EndDate = "2026-11-13"
        });
        request.Hours.Should().Be(30);
        await OkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{request.Id}/approve", employerToken);

        var booked = await OkAsync<List<Application.TimeEntries.Dtos.TimeEntryDto>>(
            HttpMethod.Get, "/api/timeentries?from=2026-11-09&to=2026-11-13", employeeToken);
        booked.Select(e => e.WorkDate).Should().HaveCount(4).And.NotContain(new DateOnly(2026, 11, 11));

        // The holiday isn't expected work: the leave week is exactly on target.
        var overview = await OkAsync<MyHoursOverviewDto>(HttpMethod.Get, "/api/hours-overview/me?asOf=2026-11-13", employeeToken);
        overview.Week.Should().BeEquivalentTo(new { ExpectedHours = 30m, WorkedHours = 30m, DifferenceHours = 0m });
        overview.Days[2].HolidayName.Should().Be("Wapenstilstand");
        (await OkAsync<List<TeamMemberHoursDto>>(HttpMethod.Get, "/api/hours-overview/team?asOf=2026-11-13", employerToken))
            .Single(r => r.UserId == employee.Id).Week.DifferenceHours.Should().Be(0);
    }
}

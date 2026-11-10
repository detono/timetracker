using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using TimeTracker.Application.Company.Dtos;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Application.Users.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>The supervisor endpoint, called with exactly the body the web frontend sends.</summary>
public class SupervisorAssignmentTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<HttpResponseMessage> PostRawAsync(Guid employeeId, string token, string body, string contentType) {
        using var request = Authorized(HttpMethod.Post, $"/api/users/{employeeId}/supervisor", token);
        request.Content = new StringContent(body, Encoding.UTF8, contentType);
        return await _client.SendAsync(request);
    }

    private async Task<Guid?> SupervisorOfAsync(Guid employeeId, string token) {
        using var request = Authorized(HttpMethod.Get, "/api/users", token);
        var users = await (await _client.SendAsync(request)).Content.ReadFromJsonAsync<List<UserDto>>();
        return users!.Single(u => u.Id == employeeId).SupervisorId;
    }

    [Fact]
    public async Task SetsAndClearsTheSupervisor_FromAJsonValue() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (lead, _, _) = await CreateEmployeeAsync(_client, employerToken);
        var (member, _, _) = await CreateEmployeeAsync(_client, employerToken);

        (await PostRawAsync(member, employerToken, $"\"{lead}\"", "application/json"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SupervisorOfAsync(member, employerToken)).Should().Be(lead);

        (await PostRawAsync(member, employerToken, "null", "application/json"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SupervisorOfAsync(member, employerToken)).Should().BeNull();
    }

    [Fact]
    public async Task RejectsTheFormEncodedBodyTheFrontendUsedToSend() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (lead, _, _) = await CreateEmployeeAsync(_client, employerToken);
        var (member, _, _) = await CreateEmployeeAsync(_client, employerToken);

        (await PostRawAsync(member, employerToken, lead.ToString(), "application/x-www-form-urlencoded"))
            .StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task ASupervisorWhoDoesntApproveLeave_SeesNoRequests_AndCantApprove() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (lead, _, leadToken) = await CreateEmployeeAsync(_client, employerToken);
        var (member, _, memberToken) = await CreateEmployeeAsync(_client, employerToken);

        using (var assign = Authorized(HttpMethod.Post, $"/api/users/{member}/supervisor?approvesLeave=false", employerToken)) {
            assign.Content = new StringContent($"\"{lead}\"", Encoding.UTF8, "application/json");
            (await _client.SendAsync(assign)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var users = await SendOkAsync<List<UserDto>>(_client, HttpMethod.Get, "/api/users", employerToken);
        users.Single(u => u.Id == member).Should().BeEquivalentTo(new { SupervisorId = lead, SupervisorApprovesLeave = false });

        var types = await SendOkAsync<List<HourTypeDto>>(_client, HttpMethod.Get, "/api/hourtypes", memberToken);
        var request = await SendOkAsync<LeaveRequestDto>(_client, HttpMethod.Post, "/api/leave-requests", memberToken, new {
            HourTypeId = types.First(t => t.IsLeave).Id, StartDate = "2026-11-16", EndDate = "2026-11-16"
        });

        // The lead still sees the hours, but not the leave request.
        (await SendAsync(_client, HttpMethod.Get, $"/api/timeentries?userId={member}&from=2026-11-16&to=2026-11-16", leadToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendOkAsync<List<LeaveRequestDto>>(_client, HttpMethod.Get, "/api/leave-requests?scope=ToReview", leadToken))
            .Should().BeEmpty();
        (await SendAsync(_client, HttpMethod.Post, $"/api/leave-requests/{request.Id}/approve", leadToken))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Choosing the same supervisor again keeps the setting; a new choice approves again.
        (await PostRawAsync(member, employerToken, $"\"{lead}\"", "application/json")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendOkAsync<List<UserDto>>(_client, HttpMethod.Get, "/api/users", employerToken))
            .Single(u => u.Id == member).SupervisorApprovesLeave.Should().BeFalse();
        (await PostRawAsync(member, employerToken, "null", "application/json")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await PostRawAsync(member, employerToken, $"\"{lead}\"", "application/json")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendOkAsync<List<UserDto>>(_client, HttpMethod.Get, "/api/users", employerToken))
            .Single(u => u.Id == member).SupervisorApprovesLeave.Should().BeTrue();
        (await SendAsync(_client, HttpMethod.Post, $"/api/leave-requests/{request.Id}/approve", leadToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TheCompanySaysWhetherEmployersGetACopy_AndOlderClientsLeaveItAlone() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);

        var company = await SendOkAsync<CompanyDto>(_client, HttpMethod.Get, "/api/company", employerToken);
        company.CopyEmployersOnLeaveRequests.Should().BeFalse();

        (await SendOkAsync<CompanyDto>(_client, HttpMethod.Put, "/api/company", employerToken,
            company with { CopyEmployersOnLeaveRequests = true })).CopyEmployersOnLeaveRequests.Should().BeTrue();

        // A body without the field (an older client) keeps it.
        (await SendOkAsync<CompanyDto>(_client, HttpMethod.Put, "/api/company", employerToken, new { company.Name }))
            .CopyEmployersOnLeaveRequests.Should().BeTrue();
    }
}

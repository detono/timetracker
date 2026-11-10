using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
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
}

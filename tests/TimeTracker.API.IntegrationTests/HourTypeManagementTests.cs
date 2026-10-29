using System.Net;
using FluentAssertions;
using TimeTracker.Application.HourTypes.Dtos;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

public class HourTypeManagementTests : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client;

    public HourTypeManagementTests(CustomWebApplicationFactory factory) {
        _client = factory.CreateClient();
    }

    private Task<List<HourTypeDto>> ListAsync(string token, bool includeInactive = false) =>
        SendOkAsync<List<HourTypeDto>>(_client, HttpMethod.Get, $"/api/hourtypes?includeInactive={includeInactive}", token);

    private static object Update(Guid id, Dictionary<string, string> names, string color = "#2a7ab0", bool isDefault = false,
        bool isLeave = false, bool deducts = false) =>
        new { Id = id, LocalizedNames = names, ColorHex = color, IsDefault = isDefault, IsLeave = isLeave, DeductsFromLeaveBalance = deducts };

    [Fact]
    public async Task EmployerEditsAnHourType_AndOnlyOneStaysTheDefault() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var created = await SendOkAsync<HourTypeDto>(_client, HttpMethod.Post, "/api/hourtypes", token, new {
            LocalizedNames = new Dictionary<string, string> { ["en"] = "Training", ["nl"] = "Opleiding" },
            ColorHex = "#123456", IsDefault = false
        });

        var updated = await SendOkAsync<HourTypeDto>(_client, HttpMethod.Put, $"/api/hourtypes/{created.Id}", token,
            Update(created.Id, new() { ["en"] = "Course", ["nl"] = "Cursus", ["fr"] = "Formation" }, "#abcdef", isDefault: true,
                isLeave: true, deducts: true));

        updated.LocalizedNames.Should().BeEquivalentTo(new Dictionary<string, string> {
            ["en"] = "Course", ["nl"] = "Cursus", ["fr"] = "Formation"
        });
        updated.ColorHex.Should().Be("#abcdef");
        updated.IsLeave.Should().BeTrue();
        updated.DeductsFromLeaveBalance.Should().BeTrue();

        var all = await ListAsync(token);
        all.Where(t => t.IsDefault).Select(t => t.Id).Should().Equal(created.Id);

        // Creating a new default takes over from the current one too.
        var newDefault = await SendOkAsync<HourTypeDto>(_client, HttpMethod.Post, "/api/hourtypes", token, new {
            LocalizedNames = new Dictionary<string, string> { ["en"] = "Office" }, ColorHex = "#654321", IsDefault = true
        });
        (await ListAsync(token)).Where(t => t.IsDefault).Select(t => t.Id).Should().Equal(newDefault.Id);
    }

    [Fact]
    public async Task NamesAreCompared_IgnoringCase() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var work = (await ListAsync(token)).First(t => !t.IsLeave);

        (await SendAsync(_client, HttpMethod.Post, "/api/hourtypes", token, new {
            LocalizedNames = new Dictionary<string, string> { ["en"] = work.LocalizedNames["en"].ToUpperInvariant() },
            ColorHex = "#123456", IsDefault = false
        })).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task NamesMustBeUnique_InEveryLanguage() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var types = await ListAsync(token);
        var work = types.First(t => !t.IsLeave);
        var other = types.First(t => t.Id != work.Id);

        // Another type's English name, used as this type's Dutch name.
        (await SendAsync(_client, HttpMethod.Put, $"/api/hourtypes/{other.Id}", token,
            Update(other.Id, new() { ["en"] = "Something new", ["nl"] = work.LocalizedNames["en"] })))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Keeping its own name is fine.
        (await SendAsync(_client, HttpMethod.Put, $"/api/hourtypes/{work.Id}", token,
            Update(work.Id, work.LocalizedNames, work.ColorHex, work.IsDefault)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivatedTypesDisappearForLogging_UntilReactivated() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(_client, token);
        var created = await SendOkAsync<HourTypeDto>(_client, HttpMethod.Post, "/api/hourtypes", token, new {
            LocalizedNames = new Dictionary<string, string> { ["en"] = "Overtime" }, ColorHex = "#123456", IsDefault = false
        });

        (await SendAsync(_client, HttpMethod.Delete, $"/api/hourtypes/{created.Id}", token)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(employee.Token)).Should().NotContain(t => t.Id == created.Id);
        (await ListAsync(token, includeInactive: true)).Should().Contain(t => t.Id == created.Id && !t.IsActive);
        (await SendAsync(_client, HttpMethod.Post, "/api/timeentries", employee.Token, new {
            HourTypeId = created.Id, WorkDate = "2026-10-05", StartTime = "09:00", EndTime = "10:00", BreakMinutes = 0
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await SendAsync(_client, HttpMethod.Post, $"/api/hourtypes/{created.Id}/activate", token)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(employee.Token)).Should().Contain(t => t.Id == created.Id);
    }

    [Fact]
    public async Task OnlyTheCompanysEmployerCanChangeItsTypes() {
        var tenant = await CreateTenantAsync(_client);
        var other = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var otherToken = await LoginAsync(_client, other.EmployerEmail, Password);
        var employee = await CreateEmployeeAsync(_client, token);
        var work = (await ListAsync(token)).First(t => !t.IsLeave);
        var body = Update(work.Id, new() { ["en"] = "Renamed" });

        (await SendAsync(_client, HttpMethod.Put, $"/api/hourtypes/{work.Id}", employee.Token, body))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(_client, HttpMethod.Put, $"/api/hourtypes/{work.Id}", otherToken, body))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(_client, HttpMethod.Post, $"/api/hourtypes/{work.Id}/activate", otherToken))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(_client, HttpMethod.Put, $"/api/hourtypes/{Guid.NewGuid()}", token, body))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ListAsync(token)).Should().Contain(t => t.Id == work.Id && t.LocalizedNames["en"] != "Renamed");
    }
}

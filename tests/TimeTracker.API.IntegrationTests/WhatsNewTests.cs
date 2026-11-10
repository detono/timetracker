using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TimeTracker.Application.WhatsNew;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>The "What's new" notes, read from the CHANGELOG.md built into the app.</summary>
public class WhatsNewTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<List<string>> UnseenAsync(string token) {
        var notes = await SendOkAsync<WhatsNewDto>(_client, HttpMethod.Get, "/api/whats-new", token);
        return notes.Releases.Select(r => r.Version).ToList();
    }

    private async Task<HttpStatusCode> MarkSeenAsync(string token, string version) {
        using var request = Authorized(HttpMethod.Post, "/api/whats-new/seen", token);
        request.Content = JsonContent.Create(new { version });
        return (await _client.SendAsync(request)).StatusCode;
    }

    [Fact]
    public async Task ANewAccount_SeesNothing_UntilThereIsSomethingNewForThem() {
        var tenant = await CreateTenantAsync(_client);
        var (_, _, token) = await CreateEmployeeAsync(_client, await LoginAsync(_client, tenant.EmployerEmail, Password));

        // The account is newer than every release in the changelog.
        (await UnseenAsync(token)).Should().BeEmpty();

        // Someone who last saw 1.2.0 gets every release after it, newest first.
        (await MarkSeenAsync(token, "1.2.0")).Should().Be(HttpStatusCode.NoContent);
        var unseen = await UnseenAsync(token);
        unseen.Should().Contain(["1.3.2", "1.3.1", "1.3.0"]).And.NotContain("1.2.0");

        (await MarkSeenAsync(token, unseen[0])).Should().Be(HttpStatusCode.NoContent);
        (await UnseenAsync(token)).Should().BeEmpty();
    }

    [Fact]
    public async Task LeavesOutDeveloperNotes_AndUnreleasedWork() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);
        (await MarkSeenAsync(token, "0.1")).Should().Be(HttpStatusCode.NoContent);

        var notes = await SendOkAsync<WhatsNewDto>(_client, HttpMethod.Get, "/api/whats-new", token);

        notes.Releases.Should().NotBeEmpty();
        notes.Releases.SelectMany(r => r.Sections).Should()
            .NotContain(s => s.Title == ReleaseNotes.DeveloperSection);
        notes.Releases.Should().NotContain(r => r.Version == "Unreleased");
    }

    [Fact]
    public async Task AnUnknownVersion_IsNotFound() {
        var tenant = await CreateTenantAsync(_client);
        var token = await LoginAsync(_client, tenant.EmployerEmail, Password);

        (await MarkSeenAsync(token, "99.0.0")).Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NeedsSigningIn() {
        (await _client.GetAsync("/api/whats-new")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

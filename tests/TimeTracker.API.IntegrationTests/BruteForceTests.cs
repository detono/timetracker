using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace TimeTracker.API.IntegrationTests;

public class LowRateLimitFactory : CustomWebApplicationFactory {
    protected override int AuthRequestsPerMinute => 3;
}

/// <summary>One address can only try a few sign-ins or reset codes per minute.</summary>
public class BruteForceTests : IClassFixture<LowRateLimitFactory> {
    private readonly HttpClient _client;

    public BruteForceTests(LowRateLimitFactory factory) {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TooManyAuthRequestsFromOneAddress_AreRefusedWith429() {
        for (var i = 0; i < 3; i++) {
            (await _client.PostAsJsonAsync("/api/auth/login", new { Email = $"guess{i}@nowhere.test", Password = "Wrong123!" }))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var refused = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "guess@nowhere.test", Password = "Wrong123!" });
        refused.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await refused.Content.ReadAsStringAsync()).Should().Contain("Too many attempts");

        // The reset endpoints share the same limit.
        (await _client.PostAsJsonAsync("/api/auth/password-reset", new { Email = "guess@nowhere.test" }))
            .StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Other endpoints aren't limited.
        (await _client.GetAsync("/api/auth/password-reset")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

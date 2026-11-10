using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TimeTracker.API.Controllers;
using TimeTracker.API.Services;
using Xunit;

namespace TimeTracker.API.IntegrationTests;

public class BrandedFactory : CustomWebApplicationFactory {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) => {
            config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Branding:Title"] = "  Shiftlog ",
                ["Branding:ColorPrimary"] = "#932E4A",
                ["Branding:ColorOnPrimary"] = "#fff",
                // An unfilled env var or a typo must not reach clients as a colour.
                ["Branding:ColorSecondary"] = "",
                ["Branding:ColorOnSecondary"] = "red; background:url(x)"
            });
        });
    }
}

public class BrandingTests(BrandedFactory factory) : IClassFixture<BrandedFactory> {
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Info_IncludesBranding_WithoutSigningIn() {
        var response = await _client.GetAsync("/api/info");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var info = (await response.Content.ReadFromJsonAsync<ServerInfoDto>())!;
        info.Branding.Title.Should().Be("Shiftlog");
        info.Branding.ColorPrimary.Should().Be("#932e4a");
        info.Branding.ColorOnPrimary.Should().Be("#fff");
    }

    [Fact]
    public async Task Info_LeavesOutBlankOrInvalidColours() {
        var info = (await _client.GetFromJsonAsync<ServerInfoDto>("/api/info"))!;

        info.Branding.ColorSecondary.Should().BeNull();
        info.Branding.ColorOnSecondary.Should().BeNull();
    }
}

public class UnbrandedInfoTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory> {
    [Fact]
    public async Task Info_WithoutBrandingConfigured_ReturnsEmptyBranding() {
        var info = (await factory.CreateClient().GetFromJsonAsync<ServerInfoDto>("/api/info"))!;

        info.Branding.Should().BeEquivalentTo(new { Title = (string?)null, ColorPrimary = (string?)null });
    }
}

public class EmailBrandingTests {
    [Fact]
    public void EmailBranding_FallsBackToTheDefaultName() {
        var branding = new AppBranding(Microsoft.Extensions.Options.Options.Create(new BrandingSettings { Title = " " }));

        branding.Title.Should().Be("TimeTracker");
    }
}

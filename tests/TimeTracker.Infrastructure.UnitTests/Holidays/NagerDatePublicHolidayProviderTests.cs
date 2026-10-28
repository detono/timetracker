using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Infrastructure.Holidays;
using TimeTracker.Infrastructure.UnitTests.Email;
using Xunit;

namespace TimeTracker.Infrastructure.UnitTests.Holidays;

/// <summary>Parses Nager.Date answers from a fake HTTP handler; nothing here calls the real service.</summary>
public class NagerDatePublicHolidayProviderTests {
    // Trimmed real answers: Belgium has nationwide holidays and some bank days, Germany regional ones.
    private const string Answer = """
        [
          {"date":"2026-11-11","localName":"Wapenstilstand","name":"Armistice Day","global":true,"counties":null,"types":["Public"]},
          {"date":"2026-12-26","localName":"Boxing Day","name":"St. Stephen's Day","global":true,"counties":null,"types":["Bank"]},
          {"date":"2026-01-06","localName":"Heilige Drei Könige","name":"Epiphany","global":false,"counties":["DE-BW","DE-BY"],"types":["Public"]}
        ]
        """;

    private static NagerDatePublicHolidayProvider Provider(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri(NagerDatePublicHolidayProvider.DefaultBaseUrl) },
            NullLogger<NagerDatePublicHolidayProvider>.Instance);

    [Fact]
    public async Task GetAsync_KeepsOnlyPublicHolidaysWithTheirLocalNameAndRegions() {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, Answer);

        var holidays = await Provider(handler).GetAsync("be", 2026);

        handler.Requests.Single().Request.RequestUri!.AbsoluteUri
            .Should().Be("https://date.nager.at/api/v3/PublicHolidays/2026/BE");
        holidays.Should().HaveCount(2, "bank holidays aren't official days off");
        holidays[0].Should().BeEquivalentTo(new { Date = new DateOnly(2026, 11, 11), Name = "Wapenstilstand", Regions = (string[]?)null });
        holidays[1].Regions.Should().Equal("DE-BW", "DE-BY");
    }

    [Fact]
    public async Task GetAsync_UnknownCountry_ExplainsIt() {
        var act = () => Provider(FakeHttpMessageHandler.Returning(HttpStatusCode.NotFound)).GetAsync("XX", 2026);

        (await act.Should().ThrowAsync<PublicHolidayLookupException>()).WithMessage("*XX*by hand*");
    }

    [Fact]
    public async Task GetAsync_ServiceUnreachable_ExplainsIt() {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("no route"));

        var act = () => Provider(handler).GetAsync("BE", 2026);

        (await act.Should().ThrowAsync<PublicHolidayLookupException>()).WithMessage("Couldn't reach*");
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;

namespace TimeTracker.Infrastructure.Holidays;

/// <summary>
/// Public holidays from Nager.Date (https://date.nager.at), a free service with no account
/// or key. Only official public holidays are returned; bank holidays and observances are
/// left out. The base address is configurable through <c>PublicHolidays:BaseUrl</c>.
/// </summary>
public class NagerDatePublicHolidayProvider(
    HttpClient http,
    ILogger<NagerDatePublicHolidayProvider> logger
) : IPublicHolidayProvider {
    public const string DefaultBaseUrl = "https://date.nager.at/";

    private record NagerHoliday(string Date, string LocalName, string Name, string[]? Counties, string[]? Types);

    public async Task<IReadOnlyList<ExternalPublicHoliday>> GetAsync(
        string countryCode, int year, CancellationToken cancellationToken = default
    ) {
        HttpResponseMessage response;
        try {
            response = await http.GetAsync($"api/v3/PublicHolidays/{year}/{countryCode.ToUpperInvariant()}", cancellationToken);
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            logger.LogWarning(ex, "Public holidays for {Country} {Year} could not be fetched.", countryCode, year);
            throw new PublicHolidayLookupException(
                "Couldn't reach the public holiday service. Try again later, or add the holidays by hand.", ex);
        }

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent) {
            throw new PublicHolidayLookupException(
                $"No public holidays are known for country {countryCode.ToUpperInvariant()}. Add them by hand.");
        }

        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Public holiday service returned {Status} for {Country} {Year}.",
                (int)response.StatusCode, countryCode, year);
            throw new PublicHolidayLookupException(
                "The public holiday service didn't answer properly. Try again later, or add the holidays by hand.");
        }

        var holidays = await response.Content.ReadFromJsonAsync<NagerHoliday[]>(cancellationToken) ?? [];
        return holidays
            .Where(h => h.Types?.Contains("Public") ?? true)
            .Select(h => new ExternalPublicHoliday(
                DateOnly.Parse(h.Date),
                string.IsNullOrWhiteSpace(h.LocalName) ? h.Name : h.LocalName,
                h.Counties is { Length: > 0 } counties ? counties : null))
            .ToList();
    }
}

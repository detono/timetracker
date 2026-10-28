using TimeTracker.Application.Common.Exceptions;

namespace TimeTracker.Application.Common.Interfaces;

/// <param name="Regions">ISO 3166-2 subdivisions it applies to; null when it's nationwide.</param>
public record ExternalPublicHoliday(DateOnly Date, string Name, IReadOnlyList<string>? Regions);

/// <summary>Looks up a country's official public holidays from an outside source.</summary>
public interface IPublicHolidayProvider
{
    /// <exception cref="PublicHolidayLookupException">With a message that can be shown to the employer.</exception>
    Task<IReadOnlyList<ExternalPublicHoliday>> GetAsync(string countryCode, int year, CancellationToken cancellationToken = default);
}

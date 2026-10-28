namespace TimeTracker.Application.Company.Dtos;

/// <param name="CountryCode">ISO 3166-1 alpha-2, e.g. "BE"; decides which public holidays apply.</param>
/// <param name="Region">ISO 3166-2, e.g. "DE-BY", for countries with regional holidays.</param>
public record CompanyDto(
    string Name,
    string? Street,
    string? PostalCode,
    string? City,
    string? CountryCode,
    string? Region);

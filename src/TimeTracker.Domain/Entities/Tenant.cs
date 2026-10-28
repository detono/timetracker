using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A customer company. Every user (except platform admins), hour type, project and time
/// entry belongs to exactly one tenant, and tenants never see each other's data.
/// Deactivating a tenant blocks login for all of its users without deleting anything.
/// </summary>
public class Tenant : BaseEntity {
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    public string? Street { get; private set; }
    public string? PostalCode { get; private set; }
    public string? City { get; private set; }

    /// <summary>ISO 3166-1 alpha-2 code, e.g. "BE". Decides which public holidays apply.</summary>
    public string? CountryCode { get; private set; }

    /// <summary>
    /// ISO 3166-2 subdivision, e.g. "DE-BY", for countries where public holidays differ per
    /// region. Null means only nationwide holidays.
    /// </summary>
    public string? Region { get; private set; }

    private Tenant() {
        // EF Core
    }

    private Tenant(string name) {
        Rename(name);
    }

    public static Tenant Create(string name) => new(name);

    public void Rename(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new DomainException("A tenant must have a name.");
        }

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetAddress(string? street, string? postalCode, string? city, string? countryCode, string? region) {
        var country = Normalize(countryCode)?.ToUpperInvariant();
        if (country is not null && (country.Length != 2 || !country.All(char.IsAsciiLetter))) {
            throw new DomainException("The country must be a two-letter code, e.g. BE.");
        }

        var subdivision = Normalize(region)?.ToUpperInvariant();
        if (subdivision is not null && (country is null || !subdivision.StartsWith(country + "-"))) {
            throw new DomainException($"The region must be a code like {country ?? "DE"}-XX for the chosen country.");
        }

        Street = Normalize(street);
        PostalCode = Normalize(postalCode);
        City = Normalize(city);
        CountryCode = country;
        Region = subdivision;
        MarkUpdated();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Deactivate() {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate() {
        IsActive = true;
        MarkUpdated();
    }
}

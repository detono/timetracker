using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A day nobody in the company is expected to work: a public holiday, or a company day
/// off. It isn't counted as expected work, and leave over it doesn't use up the balance.
/// </summary>
public class PublicHoliday : TenantOwnedEntity {
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = default!;

    private PublicHoliday() {
        // EF Core
    }

    public static PublicHoliday Create(DateOnly date, string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new DomainException("A holiday needs a name.");
        }

        return new PublicHoliday { Date = date, Name = name.Trim() };
    }
}

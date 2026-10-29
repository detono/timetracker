using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A month whose hours are final, e.g. after payroll. Nobody can add, change or remove hours
/// in it (leave bookings included) until an employer unlocks it again.
/// </summary>
public class LockedMonth : TenantOwnedEntity {
    public int Year { get; private set; }
    public int Month { get; private set; }
    public Guid LockedByUserId { get; private set; }

    public DateOnly FirstDay => new(Year, Month, 1);
    public DateOnly LastDay => FirstDay.AddMonths(1).AddDays(-1);

    private LockedMonth() {
        // EF Core
    }

    /// <param name="today">Months that haven't started yet can't be locked.</param>
    public static LockedMonth Create(int year, int month, Guid lockedByUserId, DateOnly today) {
        if (month is < 1 or > 12 || year is < 2000 or > 9999) {
            throw new DomainException("That isn't a valid month.");
        }

        if (new DateOnly(year, month, 1) > today) {
            throw new DomainException("A month can only be locked once it has started.");
        }

        if (lockedByUserId == Guid.Empty) {
            throw new DomainException("Who locked the month is required.");
        }

        return new LockedMonth { Year = year, Month = month, LockedByUserId = lockedByUserId };
    }

    /// <summary>A number that orders months, for range queries: year × 12 + month.</summary>
    public static int Index(DateOnly date) => date.Year * 12 + date.Month;
}

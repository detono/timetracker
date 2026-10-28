using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// How many leave hours someone gets in a calendar year. What they've used is not stored
/// here: it's the hours they logged that year under hour types that deduct from the balance.
/// </summary>
public class LeaveBalance : TenantOwnedEntity {
    public Guid UserId { get; private set; }
    public int Year { get; private set; }
    public decimal AllowanceHours { get; private set; }

    private LeaveBalance() {
        // EF Core
    }

    public static LeaveBalance Create(Guid userId, int year, decimal allowanceHours) {
        if (userId == Guid.Empty) {
            throw new DomainException("A leave balance must belong to a user.");
        }

        if (year is < 2000 or > 2100) {
            throw new DomainException("The year is out of range.");
        }

        var balance = new LeaveBalance { UserId = userId, Year = year };
        balance.SetAllowance(allowanceHours);
        return balance;
    }

    public void SetAllowance(decimal allowanceHours) {
        if (allowanceHours is < 0 or > 10_000) {
            throw new DomainException("The leave allowance must be between 0 and 10,000 hours.");
        }

        AllowanceHours = allowanceHours;
        MarkUpdated();
    }
}

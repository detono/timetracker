namespace TimeTracker.Application.LeaveBalances.Dtos;

/// <param name="AllowanceHours">Null when no allowance was set for this year.</param>
/// <param name="UsedHours">Hours logged this year under hour types that deduct from the balance.</param>
/// <param name="PendingHours">Hours in pending requests that would deduct from the balance.</param>
/// <param name="RemainingHours">Allowance minus used; null without an allowance. Can be negative.</param>
public record LeaveBalanceDto(
    Guid UserId,
    string UserFullName,
    int Year,
    decimal? AllowanceHours,
    decimal UsedHours,
    decimal PendingHours,
    decimal? RemainingHours);

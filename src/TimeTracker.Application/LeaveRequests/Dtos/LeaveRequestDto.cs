namespace TimeTracker.Application.LeaveRequests.Dtos;

/// <param name="CanReview">Whether the caller may approve, reject or revoke this request.</param>
/// <param name="CanCancel">Whether the caller may cancel this request.</param>
/// <param name="RemainingBalanceHours">
/// For a pending request that uses up the balance: what the requester has left in the
/// request's start year before it's booked. Null when no allowance is set or not relevant.
/// </param>
/// <param name="ExceedsBalance">Booking this pending request would take the requester below zero.</param>
public record LeaveRequestDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    Guid HourTypeId,
    Dictionary<string, string> LocalizedHourTypeNames,
    string HourTypeColor,
    DateOnly StartDate,
    DateOnly EndDate,
    string FirstDayPart,
    string LastDayPart,
    decimal Hours,
    string? Comment,
    string Status,
    string? ReviewedByName,
    DateTime? ReviewedAtUtc,
    string? ReviewComment,
    DateTime CreatedAtUtc,
    bool CanReview,
    bool CanCancel,
    decimal? RemainingBalanceHours,
    bool ExceedsBalance);

/// <summary>What a leave request for a date range would amount to, before submitting it.</summary>
/// <param name="WorkingDays">Working days covered; a half day counts as 0.5.</param>
public record LeavePreviewDto(
    decimal WorkingDays,
    decimal Hours,
    bool DeductsFromBalance,
    decimal? RemainingBalanceHours,
    bool ExceedsBalance,
    bool OverlapsExistingRequest);

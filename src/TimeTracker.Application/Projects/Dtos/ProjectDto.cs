namespace TimeTracker.Application.Projects.Dtos;

/// <param name="MemberIds">
/// Who may log hours on it; empty means everyone. Only filled in for employers.
/// </param>
/// <param name="CanLog">Whether the caller may log their own hours on it.</param>
public record ProjectDto(
    Guid Id,
    string Name,
    string? ClientName,
    bool IsActive,
    double TotalHoursLifetime,
    double TotalHoursThisMonth,
    decimal? BudgetHours,
    bool IsBillable,
    IReadOnlyList<Guid> MemberIds,
    bool CanLog
);

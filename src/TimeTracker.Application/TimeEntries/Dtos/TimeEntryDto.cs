namespace TimeTracker.Application.TimeEntries.Dtos;

public record TimeEntryDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    DateOnly WorkDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int BreakMinutes,
    double DurationHours,
    string? Notes);

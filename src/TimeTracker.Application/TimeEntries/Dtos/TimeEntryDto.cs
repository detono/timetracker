namespace TimeTracker.Application.TimeEntries.Dtos;

public record TimeEntryDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    Guid HourTypeId,
    string HourTypeName,
    string HourTypeColor,
    DateOnly WorkDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int BreakMinutes,
    double DurationHours,
    string? Notes,
    Guid? ProjectId,
    string? ProjectName
);

using TimeTracker.Application.Leave;

namespace TimeTracker.Application.WorkSchedules.Dtos;

public record WorkScheduleDayDto(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int BreakMinutes);

/// <param name="UserId">The person this is for; null for the company schedule.</param>
/// <param name="Source">Where these hours come from: the person's own schedule, the company's, or the built-in one.</param>
public record WorkScheduleDto(
    Guid? UserId,
    WorkScheduleSource Source,
    IReadOnlyList<WorkScheduleDayDto> Days,
    double WeeklyHours);

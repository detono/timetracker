namespace TimeTracker.Application.WorkSchedules.Dtos;

public record WorkScheduleDayDto(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int BreakMinutes);

/// <param name="IsDefault">True when nothing was saved yet and the default schedule applies.</param>
public record WorkScheduleDto(Guid UserId, bool IsDefault, IReadOnlyList<WorkScheduleDayDto> Days, double WeeklyHours);

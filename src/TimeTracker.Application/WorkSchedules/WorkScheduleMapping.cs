using TimeTracker.Application.Leave;
using TimeTracker.Application.WorkSchedules.Dtos;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.WorkSchedules;

internal static class WorkScheduleMapping {
    public static WorkScheduleDto ToDto(this WorkSchedule schedule, Guid? userId, WorkScheduleSource source) => new(
        userId,
        source,
        schedule.Days.Select(d => new WorkScheduleDayDto(d.DayOfWeek, d.StartTime, d.EndTime, d.BreakMinutes)).ToList(),
        Math.Round(schedule.WeeklyDuration.TotalHours, 2));
}

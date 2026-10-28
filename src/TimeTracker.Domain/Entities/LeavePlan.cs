using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>One day of leave as it gets booked: the hours taken off, and what fraction of the working day that is.</summary>
public record LeaveDay(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, int BreakMinutes, decimal Fraction) {
    public TimeSpan Duration => (EndTime - StartTime) - TimeSpan.FromMinutes(BreakMinutes);
}

/// <summary>
/// Turns a leave period into the hours taken off each working day. A half day is half of
/// the scheduled hours, without a break: the morning half starts when the day starts, the
/// afternoon half ends when the day ends. On a 9:00-17:00 day with 30 minutes' break
/// (7.5 hours), a morning is 9:00-12:45 and an afternoon 13:15-17:00.
/// </summary>
public static class LeavePlan {
    /// <summary>
    /// A single day can be a full day, a morning or an afternoon. Over several days, only the
    /// first day can be an afternoon (leave starts at noon) and only the last day a morning
    /// (leave ends at noon).
    /// </summary>
    public static void EnsureValidParts(DateOnly start, DateOnly end, DayPart firstDayPart, DayPart lastDayPart) {
        if (start == end) {
            return;
        }

        if (firstDayPart == DayPart.Morning) {
            throw new DomainException("Leave over several days can't start with only a morning; choose the whole day or the afternoon.");
        }

        if (lastDayPart == DayPart.Afternoon) {
            throw new DomainException("Leave over several days can't end with only an afternoon; choose the whole day or the morning.");
        }
    }

    public static IReadOnlyList<LeaveDay> Plan(
        WorkSchedule schedule, DateOnly start, DateOnly end,
        DayPart firstDayPart, DayPart lastDayPart, IReadOnlySet<DateOnly>? holidays = null
    ) {
        EnsureValidParts(start, end, firstDayPart, lastDayPart);

        return schedule.WorkingDaysBetween(start, end, holidays)
            .Select(d => {
                var part = d.Date == start ? firstDayPart : d.Date == end ? lastDayPart : DayPart.Full;
                return Book(d.Date, d.Hours, part);
            })
            .ToList();
    }

    private static LeaveDay Book(DateOnly date, WorkScheduleDay hours, DayPart part) {
        if (part == DayPart.Full) {
            return new LeaveDay(date, hours.StartTime, hours.EndTime, hours.BreakMinutes, 1m);
        }

        var half = TimeSpan.FromTicks(hours.Duration.Ticks / 2);
        return part == DayPart.Morning
            ? new LeaveDay(date, hours.StartTime, hours.StartTime.Add(half), 0, 0.5m)
            : new LeaveDay(date, hours.EndTime.Add(-half), hours.EndTime, 0, 0.5m);
    }
}

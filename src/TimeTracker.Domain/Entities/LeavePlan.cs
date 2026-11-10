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
    /// A single day can be a full day, a morning, an afternoon or a few hours (from a start to
    /// an end time). Over several days, only the first day can be an afternoon (leave starts
    /// at noon) and only the last day a morning (leave ends at noon).
    /// </summary>
    public static void EnsureValidParts(
        DateOnly start, DateOnly end, DayPart firstDayPart, DayPart lastDayPart,
        TimeOnly? startTime = null, TimeOnly? endTime = null
    ) {
        if (firstDayPart == DayPart.Hours || lastDayPart == DayPart.Hours) {
            if (start != end) {
                throw new DomainException("Leave for a few hours must be on a single day.");
            }

            if (startTime is not { } from || endTime is not { } to) {
                throw new DomainException("Choose the time your leave starts and ends.");
            }

            if (to <= from) {
                throw new DomainException("The end time must be after the start time.");
            }

            return;
        }

        if (startTime is not null || endTime is not null) {
            throw new DomainException("Start and end times are only used for leave of a few hours.");
        }

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
        DayPart firstDayPart, DayPart lastDayPart, IReadOnlySet<DateOnly>? holidays = null,
        TimeOnly? startTime = null, TimeOnly? endTime = null
    ) {
        if (start == end) {
            lastDayPart = firstDayPart;
        }

        EnsureValidParts(start, end, firstDayPart, lastDayPart, startTime, endTime);

        return schedule.WorkingDaysBetween(start, end, holidays)
            .Select(d => {
                var part = d.Date == start ? firstDayPart : d.Date == end ? lastDayPart : DayPart.Full;
                return part == DayPart.Hours
                    ? BookHours(d.Date, d.Hours, startTime!.Value, endTime!.Value)
                    : Book(d.Date, d.Hours, part);
            })
            .ToList();
    }

    /// <summary>The days a request books, by the schedule and holidays as they are now.</summary>
    public static IReadOnlyList<LeaveDay> Plan(WorkSchedule schedule, LeaveRequest request, IReadOnlySet<DateOnly>? holidays = null) =>
        Plan(schedule, request.StartDate, request.EndDate, request.FirstDayPart, request.LastDayPart, holidays,
            request.StartTime, request.EndTime);

    private static LeaveDay Book(DateOnly date, WorkScheduleDay hours, DayPart part) {
        if (part == DayPart.Full) {
            return new LeaveDay(date, hours.StartTime, hours.EndTime, hours.BreakMinutes, 1m);
        }

        var half = TimeSpan.FromTicks(hours.Duration.Ticks / 2);
        return part == DayPart.Morning
            ? new LeaveDay(date, hours.StartTime, hours.StartTime.Add(half), 0, 0.5m)
            : new LeaveDay(date, hours.EndTime.Add(-half), hours.EndTime, 0, 0.5m);
    }

    /// <summary>
    /// A few hours, inside the working day. They count in full, unless they're longer than the
    /// day's working time: then the difference is the break, so 9:00-17:00 on a 7.5-hour day
    /// counts as that day's 7.5 hours.
    /// </summary>
    private static LeaveDay BookHours(DateOnly date, WorkScheduleDay hours, TimeOnly from, TimeOnly to) {
        if (from < hours.StartTime || to > hours.EndTime) {
            throw new DomainException(
                $"Choose times within the working day of {date:ddd d MMM}, {hours.StartTime:HH\\:mm}-{hours.EndTime:HH\\:mm}.");
        }

        var length = to - from;
        var breakMinutes = Math.Max(0, (int)Math.Ceiling((length - hours.Duration).TotalMinutes));
        var duration = length - TimeSpan.FromMinutes(breakMinutes);
        var fraction = Math.Round((decimal)(duration.TotalMinutes / hours.Duration.TotalMinutes), 2);
        return new LeaveDay(date, from, to, breakMinutes, fraction);
    }
}

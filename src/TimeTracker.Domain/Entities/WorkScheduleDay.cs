using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>The hours someone works on one weekday. A weekday without one is a day off.</summary>
public record WorkScheduleDay {
    public DayOfWeek DayOfWeek { get; private init; }
    public TimeOnly StartTime { get; private init; }
    public TimeOnly EndTime { get; private init; }
    public int BreakMinutes { get; private init; }

    private WorkScheduleDay() {
        // EF Core
    }

    public WorkScheduleDay(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, int breakMinutes) {
        if (endTime <= startTime) {
            throw new DomainException($"{dayOfWeek}: end time must be after start time.");
        }

        if (breakMinutes < 0 || breakMinutes >= (endTime - startTime).TotalMinutes) {
            throw new DomainException($"{dayOfWeek}: the break must be shorter than the working day.");
        }

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes;
    }

    /// <summary>Net working time, excluding the break.</summary>
    public TimeSpan Duration => (EndTime - StartTime) - TimeSpan.FromMinutes(BreakMinutes);
}

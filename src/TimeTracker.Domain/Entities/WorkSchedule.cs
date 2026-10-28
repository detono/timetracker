using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// The weekly hours a person normally works. Used to turn a leave request ("Monday to
/// Friday") into the hours they would actually have worked on each of those days.
/// Someone without a saved schedule is assumed to work <see cref="Default"/>.
/// </summary>
public class WorkSchedule : TenantOwnedEntity {
    public Guid UserId { get; private set; }

    private List<WorkScheduleDay> _days = new();
    public IReadOnlyList<WorkScheduleDay> Days => _days.AsReadOnly();

    private WorkSchedule() {
        // EF Core
    }

    public static WorkSchedule Create(Guid userId, IEnumerable<WorkScheduleDay> days) {
        if (userId == Guid.Empty) {
            throw new DomainException("A work schedule must belong to a user.");
        }

        var schedule = new WorkSchedule { UserId = userId };
        schedule.SetDays(days);
        return schedule;
    }

    /// <summary>Monday to Friday, 09:00 to 17:00 with a 30-minute break (7.5 hours a day).</summary>
    public static IReadOnlyList<WorkScheduleDay> DefaultDays { get; } = new[] {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    }.Select(d => new WorkScheduleDay(d, new TimeOnly(9, 0), new TimeOnly(17, 0), 30)).ToList();

    /// <summary>An unsaved schedule with <see cref="DefaultDays"/>, for people who don't have one yet.</summary>
    public static WorkSchedule Default(Guid userId) => Create(userId, DefaultDays);

    public void SetDays(IEnumerable<WorkScheduleDay> days) {
        var list = days.ToList();
        if (list.GroupBy(d => d.DayOfWeek).Any(g => g.Count() > 1)) {
            throw new DomainException("Each weekday can only appear once in a work schedule.");
        }

        _days = list.OrderBy(d => ((int)d.DayOfWeek + 6) % 7).ToList(); // Monday first
        MarkUpdated();
    }

    /// <summary>The working hours on <paramref name="date"/>, or null if it's a day off.</summary>
    public WorkScheduleDay? DayFor(DateOnly date) => _days.FirstOrDefault(d => d.DayOfWeek == date.DayOfWeek);

    /// <summary>Every working day between the two dates (inclusive) with its scheduled hours.</summary>
    public IReadOnlyList<(DateOnly Date, WorkScheduleDay Hours)> WorkingDaysBetween(DateOnly start, DateOnly end) {
        var result = new List<(DateOnly, WorkScheduleDay)>();
        for (var date = start; date <= end; date = date.AddDays(1)) {
            if (DayFor(date) is { } hours) {
                result.Add((date, hours));
            }
        }

        return result;
    }

    public TimeSpan WeeklyDuration => TimeSpan.FromTicks(_days.Sum(d => d.Duration.Ticks));
}

using TimeTracker.Application.HoursOverview.Dtos;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.HoursOverview;

/// <summary>
/// Compares logged hours with scheduled hours. Nothing is stored: every number is
/// recalculated from time entries and the work schedule each time it's asked for.
/// <list type="bullet">
/// <item>Expected hours count up to yesterday, plus today once anything is logged for today, so
/// you don't look "behind" every morning before you've logged the day.</item>
/// <item>All logged hours count as worked, including leave and sick days.</item>
/// <item>Expected hours start when someone started: the day their account was created, or the first
/// day hours were logged for them if that's earlier (e.g. an employer backfilling hours).</item>
/// <item>The current schedule is used for the whole period; past schedule changes aren't remembered.</item>
/// <item>Public holidays and company days off aren't expected work.</item>
/// </list>
/// </summary>
public static class HoursOverviewCalculator {
    public static DateOnly StartOfWeek(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public static HoursPeriodDto Period(
        DateOnly from, DateOnly to, DateOnly asOf, DateOnly countFrom,
        WorkSchedule schedule, IReadOnlyCollection<TimeEntry> entries,
        IReadOnlyDictionary<DateOnly, string>? holidays = null
    ) {
        var end = to < asOf ? to : asOf;
        var expectedEnd = to < LastCountedDay(asOf, entries) ? to : LastCountedDay(asOf, entries);
        var worked = Worked(entries, from, end);
        var expected = Expected(schedule, from > countFrom ? from : countFrom, expectedEnd, holidays);
        return new HoursPeriodDto(from, end, worked, expected, worked - expected);
    }

    public static HoursPeriodDto Week(
        DateOnly asOf, DateOnly countFrom, WorkSchedule schedule, IReadOnlyCollection<TimeEntry> entries,
        IReadOnlyDictionary<DateOnly, string>? holidays = null
    ) {
        var monday = StartOfWeek(asOf);
        return Period(monday, monday.AddDays(6), asOf, countFrom, schedule, entries, holidays);
    }

    public static HoursPeriodDto Month(
        DateOnly asOf, DateOnly countFrom, WorkSchedule schedule, IReadOnlyCollection<TimeEntry> entries,
        IReadOnlyDictionary<DateOnly, string>? holidays = null
    ) {
        var first = new DateOnly(asOf.Year, asOf.Month, 1);
        return Period(first, first.AddMonths(1).AddDays(-1), asOf, countFrom, schedule, entries, holidays);
    }

    public static HoursPeriodDto Year(
        DateOnly asOf, DateOnly countFrom, WorkSchedule schedule, IReadOnlyCollection<TimeEntry> entries,
        IReadOnlyDictionary<DateOnly, string>? holidays = null
    ) =>
        Period(new DateOnly(asOf.Year, 1, 1), new DateOnly(asOf.Year, 12, 31), asOf, countFrom, schedule, entries, holidays);

    /// <summary>The last day whose expected hours count: today once something is logged for it, else yesterday.</summary>
    public static DateOnly LastCountedDay(DateOnly asOf, IEnumerable<TimeEntry> entries) =>
        entries.Any(e => e.WorkDate == asOf) ? asOf : asOf.AddDays(-1);

    /// <summary>Monday to Sunday of the week containing <paramref name="asOf"/>, including days still to come.</summary>
    public static IReadOnlyList<HoursDayDto> Days(
        DateOnly asOf, DateOnly countFrom, WorkSchedule schedule,
        IReadOnlyCollection<TimeEntry> entries, IReadOnlySet<Guid> leaveHourTypeIds,
        IReadOnlyDictionary<DateOnly, string>? holidays = null
    ) {
        var monday = StartOfWeek(asOf);
        var lastCounted = LastCountedDay(asOf, entries);
        return Enumerable.Range(0, 7).Select(i => {
            var date = monday.AddDays(i);
            var dayEntries = entries.Where(e => e.WorkDate == date).ToList();
            string? holiday = null;
            holidays?.TryGetValue(date, out holiday);
            var expected = date < countFrom || holiday is not null
                ? 0
                : Hours(schedule.DayFor(date)?.Duration ?? TimeSpan.Zero);
            return new HoursDayDto(
                date,
                Hours(dayEntries.Aggregate(TimeSpan.Zero, (sum, e) => sum + e.Duration)),
                expected,
                date > lastCounted,
                dayEntries.Any(e => leaveHourTypeIds.Contains(e.HourTypeId)),
                holiday);
        }).ToList();
    }

    /// <summary>
    /// The first day expected hours count for someone: when their account was created, or the
    /// first day hours were logged for them if that's earlier.
    /// </summary>
    public static DateOnly CountFrom(User user, IEnumerable<TimeEntry> entries) {
        var created = DateOnly.FromDateTime(user.CreatedAtUtc);
        var firstLogged = entries.Select(e => (DateOnly?)e.WorkDate).Min();
        return firstLogged is { } first && first < created ? first : created;
    }

    private static decimal Worked(IEnumerable<TimeEntry> entries, DateOnly from, DateOnly to) =>
        Hours(entries.Where(e => e.WorkDate >= from && e.WorkDate <= to)
            .Aggregate(TimeSpan.Zero, (sum, e) => sum + e.Duration));

    private static decimal Expected(
        WorkSchedule schedule, DateOnly from, DateOnly to, IReadOnlyDictionary<DateOnly, string>? holidays
    ) =>
        from > to ? 0 : Hours(schedule.WorkingDaysBetween(from, to, holidays?.Keys.ToHashSet())
            .Aggregate(TimeSpan.Zero, (sum, d) => sum + d.Hours.Duration));

    private static decimal Hours(TimeSpan duration) => Math.Round((decimal)duration.TotalHours, 2);
}

using System.Globalization;
using TimeTracker.Application.Reports.Queries.GetHoursReport;

namespace TimeTracker.Application.Reports;

/// <summary>The days, ISO weeks or months a report is split into.</summary>
public static class ReportPeriods {
    /// <summary>Every period that overlaps <paramref name="from"/> to <paramref name="to"/>, in order.</summary>
    public static IReadOnlyList<(DateOnly Start, DateOnly End, string Label)> Between(
        DateOnly from, DateOnly to, ReportGrouping grouping
    ) {
        var periods = new List<(DateOnly, DateOnly, string)>();
        var date = from;
        while (date <= to) {
            var period = Bounds(Key(date, grouping), grouping);
            periods.Add(period);
            date = period.End.AddDays(1);
        }

        return periods;
    }

    public static string Key(DateOnly date, ReportGrouping grouping) => grouping switch {
        ReportGrouping.Day => date.ToString("yyyy-MM-dd"),
        ReportGrouping.Week =>
            $"{ISOWeek.GetYear(date.ToDateTime(TimeOnly.MinValue))}-W{ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue)):D2}",
        ReportGrouping.Month => date.ToString("yyyy-MM"),
        _ => throw new ArgumentOutOfRangeException(nameof(grouping))
    };

    public static (DateOnly Start, DateOnly End, string Label) Bounds(string key, ReportGrouping grouping) {
        switch (grouping) {
            case ReportGrouping.Day:
                var day = DateOnly.ParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                return (day, day, day.ToString("yyyy-MM-dd"));

            case ReportGrouping.Week:
                var parts = key.Split("-W");
                var year = int.Parse(parts[0]);
                var week = int.Parse(parts[1]);
                var weekStart = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
                var weekEnd = weekStart.AddDays(6);
                return (DateOnly.FromDateTime(weekStart), DateOnly.FromDateTime(weekEnd), key);

            case ReportGrouping.Month:
                var monthStart = DateOnly.ParseExact(key + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                return (monthStart, monthEnd, monthStart.ToString("yyyy-MM"));

            default:
                throw new ArgumentOutOfRangeException(nameof(grouping));
        }
    }
}

using FluentAssertions;
using TimeTracker.Application.Reports;
using TimeTracker.Application.Reports.Queries.GetHoursReport;
using Xunit;

namespace TimeTracker.Application.UnitTests.Reports;

public class ReportPeriodsTests
{
    [Fact]
    public void Weeks_FollowIsoNumbering_AcrossNewYear()
    {
        // 31 Dec 2026 is a Thursday, so 2026 has a week 53; 2027 week 1 starts Monday 4 Jan.
        var weeks = ReportPeriods.Between(new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 5), ReportGrouping.Week);

        weeks.Select(w => (w.Label, w.Start, w.End)).Should().Equal(
            ("2026-W53", new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 3)),
            ("2027-W01", new DateOnly(2027, 1, 4), new DateOnly(2027, 1, 10)));
    }

    [Fact]
    public void Months_CoverWholeMonths_IncludingLeapDays()
    {
        var months = ReportPeriods.Between(new DateOnly(2028, 1, 15), new DateOnly(2028, 3, 1), ReportGrouping.Month);

        months.Select(m => (m.Label, m.Start, m.End)).Should().Equal(
            ("2028-01", new DateOnly(2028, 1, 1), new DateOnly(2028, 1, 31)),
            ("2028-02", new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29)),
            ("2028-03", new DateOnly(2028, 3, 1), new DateOnly(2028, 3, 31)));
    }

    [Fact]
    public void Days_AreOnePerDate()
    {
        var days = ReportPeriods.Between(new DateOnly(2026, 10, 30), new DateOnly(2026, 11, 1), ReportGrouping.Day);

        days.Select(d => d.Label).Should().Equal("2026-10-30", "2026-10-31", "2026-11-01");
        days.Should().OnlyContain(d => d.Start == d.End);
    }
}

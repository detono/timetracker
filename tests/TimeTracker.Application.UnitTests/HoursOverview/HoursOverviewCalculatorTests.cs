using FluentAssertions;
using TimeTracker.Application.HoursOverview;
using TimeTracker.Domain.Entities;
using Xunit;
using static TimeTracker.Application.HoursOverview.HoursOverviewCalculator;

namespace TimeTracker.Application.UnitTests.HoursOverview;

public class HoursOverviewCalculatorTests {
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid WorkType = Guid.NewGuid();
    private static readonly Guid LeaveType = Guid.NewGuid();

    // Monday to Thursday 8:30-17:30 with an hour's lunch (8h), Friday 8:30-15:30 with 30 minutes (6.5h).
    private static readonly WorkSchedule Firm = WorkSchedule.CreateCompanyDefault(
        new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }
            .Select(d => new WorkScheduleDay(d, new TimeOnly(8, 30), new TimeOnly(17, 30), 60))
            .Append(new WorkScheduleDay(DayOfWeek.Friday, new TimeOnly(8, 30), new TimeOnly(15, 30), 30)));

    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly DateOnly Wednesday = new(2026, 11, 4);
    private static readonly DateOnly LongAgo = new(2020, 1, 1);

    private static TimeEntry Entry(DateOnly date, int startHour, int endHour, int breakMinutes = 0, Guid? type = null) =>
        TimeEntry.Create(UserId, type ?? WorkType, date, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0), breakMinutes);

    [Fact]
    public void Week_OnlyExpectsHoursUpToTheReferenceDate() {
        var entries = new[] { Entry(Monday, 8, 18, 60), Entry(Monday.AddDays(1), 9, 17), Entry(Wednesday, 8, 12) };

        var week = Week(Wednesday, LongAgo, Firm, entries);

        week.From.Should().Be(Monday);
        week.To.Should().Be(Wednesday);
        week.ExpectedHours.Should().Be(24, "Monday to Wednesday, not the whole week");
        week.WorkedHours.Should().Be(21);
        week.DifferenceHours.Should().Be(-3);
    }

    [Fact]
    public void Today_OnlyCountsOnceSomethingIsLoggedForIt() {
        var mondayAndTuesday = new[] { Entry(Monday, 8, 17, 60), Entry(Monday.AddDays(1), 8, 17, 60) };

        // Wednesday morning, nothing logged yet: not behind.
        var morning = Week(Wednesday, LongAgo, Firm, mondayAndTuesday);
        morning.ExpectedHours.Should().Be(16);
        morning.DifferenceHours.Should().Be(0);
        Days(Wednesday, LongAgo, Firm, mondayAndTuesday, new HashSet<Guid>())[2].IsPending.Should().BeTrue();

        // Once Wednesday is logged, it counts in full.
        var evening = Week(Wednesday, LongAgo, Firm, mondayAndTuesday.Append(Entry(Wednesday, 8, 12)).ToList());
        evening.ExpectedHours.Should().Be(24);
        evening.DifferenceHours.Should().Be(-4);

        // A missed day still shows the next day.
        Week(Wednesday.AddDays(1), LongAgo, Firm, mondayAndTuesday).DifferenceHours.Should().Be(-8);
    }

    [Fact]
    public void Leave_CountsAsWorked() {
        var entries = new[] {
            Entry(Monday, 8, 16), Entry(Monday.AddDays(1), 8, 16), Entry(Wednesday, 8, 16, type: LeaveType)
        };

        Week(Wednesday, LongAgo, Firm, entries).DifferenceHours.Should().Be(0);
    }

    [Fact]
    public void ExpectedHours_StartWhenTheAccountWasCreated() {
        // Created on Wednesday: Monday and Tuesday aren't expected.
        var week = Week(Wednesday, Wednesday, Firm, new[] { Entry(Wednesday, 9, 17) });

        week.ExpectedHours.Should().Be(8);
        week.DifferenceHours.Should().Be(0);
    }

    [Fact]
    public void Month_AndYear_RunFromTheirFirstDayToTheReferenceDate() {
        var month = Month(Wednesday, LongAgo, Firm, Array.Empty<TimeEntry>());
        month.From.Should().Be(new DateOnly(2026, 11, 1));
        month.ExpectedHours.Should().Be(16, "1 November 2026 is a Sunday, and Wednesday isn't logged yet");

        var year = Year(Wednesday, LongAgo, Firm, Array.Empty<TimeEntry>());
        year.From.Should().Be(new DateOnly(2026, 1, 1));
        year.To.Should().Be(Wednesday);
    }

    [Fact]
    public void Days_ShowTheWholeWeekAndMarkFutureLeave() {
        var friday = Monday.AddDays(4);
        var entries = new[] { Entry(Monday, 8, 17, 60), Entry(friday, 8, 15, 30, LeaveType) };

        var days = Days(Wednesday, LongAgo, Firm, entries, new HashSet<Guid> { LeaveType });

        days.Should().HaveCount(7);
        days[0].Should().BeEquivalentTo(new { Date = Monday, WorkedHours = 8m, ExpectedHours = 8m, IsPending = false, HasLeave = false });
        days[4].Should().BeEquivalentTo(new { Date = friday, WorkedHours = 6.5m, ExpectedHours = 6.5m, IsPending = true, HasLeave = true });
        days[5].ExpectedHours.Should().Be(0, "Saturday is a day off");

        // Future leave shows on the day but isn't in the week's total yet.
        Week(Wednesday, LongAgo, Firm, entries).WorkedHours.Should().Be(8);
    }

    [Fact]
    public void Holidays_AreNotExpectedWork() {
        var holidays = new Dictionary<DateOnly, string> { [Monday.AddDays(1)] = "Company day off" };
        var entries = new[] { Entry(Monday, 8, 17, 60), Entry(Wednesday, 8, 17, 60) };

        var week = Week(Wednesday, LongAgo, Firm, entries, holidays);
        week.ExpectedHours.Should().Be(16, "Tuesday is a holiday");
        week.DifferenceHours.Should().Be(0);

        var tuesday = Days(Wednesday, LongAgo, Firm, entries, new HashSet<Guid>(), holidays)[1];
        tuesday.ExpectedHours.Should().Be(0);
        tuesday.HolidayName.Should().Be("Company day off");
    }
}

using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class LeavePlanTests {
    // Monday to Friday, 9:00-17:00 with 30 minutes' break: 7.5 hours a day.
    private static readonly WorkSchedule Schedule = WorkSchedule.BuiltInDefault();
    private static readonly DateOnly Wednesday = new(2026, 11, 4);
    private static readonly DateOnly Friday = new(2026, 11, 6);

    [Fact]
    public void HalfDays_AreHalfTheScheduledHoursWithoutBreak() {
        var morning = LeavePlan.Plan(Schedule, Wednesday, Wednesday, DayPart.Morning, DayPart.Morning).Single();
        morning.Should().BeEquivalentTo(new { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(12, 45), BreakMinutes = 0, Fraction = 0.5m });
        morning.Duration.Should().Be(TimeSpan.FromHours(3.75));

        var afternoon = LeavePlan.Plan(Schedule, Wednesday, Wednesday, DayPart.Afternoon, DayPart.Afternoon).Single();
        afternoon.Should().BeEquivalentTo(new { StartTime = new TimeOnly(13, 15), EndTime = new TimeOnly(17, 0), BreakMinutes = 0 });
    }

    [Fact]
    public void Range_CanStartInTheAfternoonAndEndAtNoon() {
        var days = LeavePlan.Plan(Schedule, Wednesday, Friday, DayPart.Afternoon, DayPart.Morning);

        days.Select(d => d.Fraction).Should().Equal(0.5m, 1m, 0.5m);
        days.Sum(d => d.Duration.TotalHours).Should().Be(15);
        days[1].BreakMinutes.Should().Be(30, "full days keep their break");
    }

    [Theory]
    [InlineData(DayPart.Morning, DayPart.Full)] // can't take only the morning, then come back for days
    [InlineData(DayPart.Full, DayPart.Afternoon)] // can't come in for the morning on the last day
    public void Range_RejectsHalfDaysOnTheWrongSide(DayPart first, DayPart last) {
        var act = () => LeavePlan.Plan(Schedule, Wednesday, Friday, first, last);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void LeaveRequest_SingleDayUsesTheSamePartForStartAndEnd() {
        var request = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Wednesday, Wednesday, 3.75m, null, DayPart.Afternoon);

        request.FirstDayPart.Should().Be(DayPart.Afternoon);
        request.LastDayPart.Should().Be(DayPart.Afternoon);
    }

    [Fact]
    public void AFewHours_BookThoseTimesWithoutBreak() {
        var day = LeavePlan.Plan(Schedule, Wednesday, Wednesday, DayPart.Hours, DayPart.Hours,
            startTime: new TimeOnly(14, 0), endTime: new TimeOnly(16, 0)).Single();

        day.Should().BeEquivalentTo(new { StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(16, 0), BreakMinutes = 0 });
        day.Duration.Should().Be(TimeSpan.FromHours(2));
        day.Fraction.Should().Be(0.27m, "2 of 7.5 hours");
    }

    [Fact]
    public void AFewHours_NeverCountMoreThanTheWorkingDay() {
        var day = LeavePlan.Plan(Schedule, Wednesday, Wednesday, DayPart.Hours, DayPart.Hours,
            startTime: new TimeOnly(9, 0), endTime: new TimeOnly(17, 0)).Single();

        day.BreakMinutes.Should().Be(30);
        day.Duration.Should().Be(TimeSpan.FromHours(7.5));
        day.Fraction.Should().Be(1m);
    }

    [Theory]
    [InlineData(8, 0, 10, 0)] // starts before the working day
    [InlineData(16, 0, 17, 30)] // ends after it
    public void AFewHours_MustFallWithinTheWorkingDay(int fromHour, int fromMinute, int toHour, int toMinute) {
        var act = () => LeavePlan.Plan(Schedule, Wednesday, Wednesday, DayPart.Hours, DayPart.Hours,
            startTime: new TimeOnly(fromHour, fromMinute), endTime: new TimeOnly(toHour, toMinute));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AFewHours_OnADayOff_BookNothing() {
        var saturday = new DateOnly(2026, 11, 7);

        LeavePlan.Plan(Schedule, saturday, saturday, DayPart.Hours, DayPart.Hours,
            startTime: new TimeOnly(10, 0), endTime: new TimeOnly(12, 0)).Should().BeEmpty();
    }

    [Fact]
    public void AFewHours_NeedASingleDayAndBothTimes_AndOtherPartsNoTimes() {
        var from = new TimeOnly(14, 0);
        var to = new TimeOnly(16, 0);

        FluentActions.Invoking(() => LeavePlan.EnsureValidParts(Wednesday, Friday, DayPart.Hours, DayPart.Full, from, to))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => LeavePlan.EnsureValidParts(Wednesday, Wednesday, DayPart.Hours, DayPart.Hours, from, null))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => LeavePlan.EnsureValidParts(Wednesday, Wednesday, DayPart.Hours, DayPart.Hours, to, from))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => LeavePlan.EnsureValidParts(Wednesday, Wednesday, DayPart.Full, DayPart.Full, from, to))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void AFewHours_OnlyConflictWithLeaveAtTheSameTime() {
        var dentist = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Wednesday, Wednesday, 2m, null,
            DayPart.Hours, DayPart.Hours, new TimeOnly(14, 0), new TimeOnly(16, 0));

        dentist.ConflictsWith(Wednesday, Wednesday, new TimeOnly(9, 0), new TimeOnly(10, 0)).Should().BeFalse();
        dentist.ConflictsWith(Wednesday, Wednesday, new TimeOnly(16, 0), new TimeOnly(17, 0)).Should().BeFalse("back to back is fine");
        dentist.ConflictsWith(Wednesday, Wednesday, new TimeOnly(15, 0), new TimeOnly(17, 0)).Should().BeTrue();
        dentist.ConflictsWith(Wednesday, Wednesday).Should().BeTrue("a whole or half day takes the day");
        dentist.ConflictsWith(Friday, Friday, new TimeOnly(14, 0), new TimeOnly(16, 0)).Should().BeFalse("another day");
    }
}

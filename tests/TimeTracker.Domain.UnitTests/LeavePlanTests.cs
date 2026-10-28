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
}

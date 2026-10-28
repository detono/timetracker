using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class WorkScheduleTests {
    private static WorkScheduleDay Day(DayOfWeek day, int startHour, int endHour, int breakMinutes = 0) =>
        new(day, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0), breakMinutes);

    [Fact]
    public void Default_IsMondayToFridayAt7AndAHalfHours() {
        var schedule = WorkSchedule.Default(Guid.NewGuid());

        schedule.Days.Select(d => d.DayOfWeek).Should().Equal(
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);
        schedule.WeeklyDuration.Should().Be(TimeSpan.FromHours(37.5));
    }

    [Fact]
    public void WorkingDaysBetween_SkipsDaysOff() {
        // Works Monday and Wednesday only.
        var schedule = WorkSchedule.Create(Guid.NewGuid(), new[] {
            Day(DayOfWeek.Wednesday, 9, 13), Day(DayOfWeek.Monday, 8, 17, 60)
        });

        // Monday 2 Nov 2026 to Sunday 8 Nov 2026.
        var days = schedule.WorkingDaysBetween(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 8));

        days.Select(d => d.Date).Should().Equal(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 4));
        days.Sum(d => d.Hours.Duration.TotalHours).Should().Be(12);
        schedule.Days[0].DayOfWeek.Should().Be(DayOfWeek.Monday, "days are kept in Monday-first order");
    }

    [Fact]
    public void SetDays_WithDuplicateWeekday_Throws() {
        var act = () => WorkSchedule.Create(Guid.NewGuid(), new[] {
            Day(DayOfWeek.Monday, 9, 12), Day(DayOfWeek.Monday, 13, 17)
        });

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(17, 9, 0)] // ends before it starts
    [InlineData(9, 10, 60)] // break as long as the day
    public void Day_WithInvalidHours_Throws(int start, int end, int breakMinutes) {
        var act = () => Day(DayOfWeek.Monday, start, end, breakMinutes);

        act.Should().Throw<DomainException>();
    }
}

using FluentAssertions;
using TimeTracker.Application.Leave;
using TimeTracker.Domain.Entities;
using Xunit;

namespace TimeTracker.Application.UnitTests.Leave;

public class LeaveServiceTests {
    [Fact]
    public void HoursInYear_SplitsARequestAcrossTheNewYear() {
        var userId = Guid.NewGuid();
        var schedule = WorkSchedule.BuiltInDefault(); // 7.5 hours, Monday to Friday

        // Mon 28 Dec 2026 to Fri 8 Jan 2027: Mon-Thu in 2026, then Fri 1 Jan and the next full week in 2027.
        var start = new DateOnly(2026, 12, 28);
        var end = new DateOnly(2027, 1, 8);
        var total = LeaveService.HoursOf(schedule.WorkingDaysBetween(start, end));
        var request = LeaveRequest.Create(userId, Guid.NewGuid(), start, end, total, null);

        var in2026 = LeaveService.HoursInYear(request, schedule, 2026);
        var in2027 = LeaveService.HoursInYear(request, schedule, 2027);

        in2026.Should().Be(4 * 7.5m); // 28-31 Dec
        in2027.Should().Be(6 * 7.5m); // 1 Jan (Fri), 4-8 Jan
        (in2026 + in2027).Should().Be(total);
        LeaveService.HoursInYear(request, schedule, 2028).Should().Be(0);
    }

    [Fact]
    public void UsedHours_CountsOnlyTypesThatDeduct() {
        var userId = Guid.NewGuid();
        var paidLeave = Guid.NewGuid();
        var sickLeave = Guid.NewGuid();
        var entries = new[] {
            TimeEntry.Create(userId, paidLeave, new DateOnly(2026, 3, 2), new TimeOnly(9, 0), new TimeOnly(17, 0), 30),
            TimeEntry.Create(userId, paidLeave, new DateOnly(2026, 3, 3), new TimeOnly(9, 0), new TimeOnly(13, 0)),
            TimeEntry.Create(userId, sickLeave, new DateOnly(2026, 3, 4), new TimeOnly(9, 0), new TimeOnly(17, 0))
        };

        LeaveService.UsedHours(entries, new HashSet<Guid> { paidLeave }).Should().Be(11.5m);
    }
}

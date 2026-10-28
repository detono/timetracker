using TimeTracker.Application.Leave;

namespace TimeTracker.Application.HoursOverview.Dtos;

/// <summary>
/// Logged versus scheduled hours over a period that ends today (or at the period's end,
/// if that's earlier). Positive <see cref="DifferenceHours"/> means more than scheduled.
/// </summary>
public record HoursPeriodDto(
    DateOnly From,
    DateOnly To,
    decimal WorkedHours,
    decimal ExpectedHours,
    decimal DifferenceHours);

/// <param name="IsPending">
/// Not counted in the totals yet: a day still to come, or today while nothing is logged for it.
/// </param>
/// <param name="HasLeave">Leave is logged or booked on this day.</param>
public record HoursDayDto(
    DateOnly Date,
    decimal WorkedHours,
    decimal ExpectedHours,
    bool IsPending,
    bool HasLeave);

/// <summary>
/// The caller's own overview: this week, month and year so far, plus the week day by day.
/// <see cref="TracksHours"/> is false for people whose hours aren't compared with a schedule.
/// </summary>
public record MyHoursOverviewDto(
    DateOnly AsOf,
    bool TracksHours,
    WorkScheduleSource ScheduleSource,
    double WeeklyScheduleHours,
    HoursPeriodDto Week,
    HoursPeriodDto Month,
    HoursPeriodDto Year,
    IReadOnlyList<HoursDayDto> Days);

/// <summary>One row of the team overview.</summary>
public record TeamMemberHoursDto(
    Guid UserId,
    string FullName,
    WorkScheduleSource ScheduleSource,
    double WeeklyScheduleHours,
    HoursPeriodDto Week,
    HoursPeriodDto Month,
    HoursPeriodDto Year);

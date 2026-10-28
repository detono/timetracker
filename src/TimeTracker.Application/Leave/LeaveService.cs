using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Leave;

/// <summary>The balance of one person for one year. Allowance is null when no allowance was set.</summary>
public record LeaveBalanceSnapshot(int Year, decimal? AllowanceHours, decimal UsedHours, decimal PendingHours) {
    public decimal? RemainingHours => AllowanceHours - UsedHours;
}

/// <summary>
/// Shared leave rules: who may review whose requests, how many hours a date range is
/// worth for someone, and what their leave balance looks like.
/// </summary>
public class LeaveService(IUnitOfWork unitOfWork, ICurrentUserService currentUser) {
    /// <summary>An employer reviews everyone's requests; a supervisor those of the people they supervise.</summary>
    public bool CanReview(User requester) =>
        currentUser.Role == UserRole.Employer || requester.SupervisorId == currentUser.UserId;

    /// <summary>The users whose requests the caller reviews: null means everyone (employers).</summary>
    public async Task<IReadOnlyList<Guid>?> ReviewableUserIdsAsync(CancellationToken cancellationToken) {
        if (currentUser.Role == UserRole.Employer) {
            return null;
        }

        var supervisees = await unitOfWork.Users.GetSupervisedByAsync(currentUser.UserId, cancellationToken);
        return supervisees.Select(u => u.Id).ToList();
    }

    public async Task<WorkSchedule> GetScheduleAsync(Guid userId, CancellationToken cancellationToken) =>
        await unitOfWork.WorkSchedules.GetForUserAsync(userId, cancellationToken) ?? WorkSchedule.Default(userId);

    public static decimal HoursOf(IEnumerable<(DateOnly Date, WorkScheduleDay Hours)> days) =>
        Math.Round((decimal)days.Sum(d => d.Hours.Duration.TotalHours), 2);

    /// <summary>The scheduled hours of <paramref name="request"/> that fall in <paramref name="year"/>.</summary>
    public static decimal HoursInYear(LeaveRequest request, WorkSchedule schedule, int year) {
        if (request.StartDate.Year == year && request.EndDate.Year == year) {
            return request.Hours;
        }

        var start = Max(request.StartDate, new DateOnly(year, 1, 1));
        var end = Min(request.EndDate, new DateOnly(year, 12, 31));
        return start > end ? 0 : HoursOf(schedule.WorkingDaysBetween(start, end));
    }

    /// <summary>Balance of one person, from their logged leave hours and pending requests.</summary>
    public async Task<LeaveBalanceSnapshot> GetBalanceAsync(
        Guid userId, int year, WorkSchedule schedule, CancellationToken cancellationToken
    ) {
        var balance = await unitOfWork.LeaveBalances.GetAsync(userId, year, cancellationToken);
        var deducting = await DeductingHourTypeIdsAsync(cancellationToken);

        var entries = await unitOfWork.TimeEntries.GetForUserAsync(
            userId, new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), cancellationToken);
        var used = UsedHours(entries, deducting);

        var pendingRequests = await unitOfWork.LeaveRequests.GetAsync(
            new[] { userId }, LeaveRequestStatus.Pending, cancellationToken);
        var pending = pendingRequests
            .Where(r => deducting.Contains(r.HourTypeId))
            .Sum(r => HoursInYear(r, schedule, year));

        return new LeaveBalanceSnapshot(year, balance?.AllowanceHours, used, pending);
    }

    /// <summary>
    /// Whether booking <paramref name="request"/> would take its owner below zero in any
    /// year it touches. Requests under a type that doesn't deduct never exceed.
    /// </summary>
    public async Task<(LeaveBalanceSnapshot? Balance, bool Exceeds)> CheckBalanceAsync(
        LeaveRequest request, HourType hourType, WorkSchedule schedule, CancellationToken cancellationToken
    ) {
        if (!hourType.DeductsFromLeaveBalance) {
            return (null, false);
        }

        LeaveBalanceSnapshot? first = null;
        var exceeds = false;
        for (var year = request.StartDate.Year; year <= request.EndDate.Year; year++) {
            var snapshot = await GetBalanceAsync(request.UserId, year, schedule, cancellationToken);
            first ??= snapshot;
            if (snapshot.RemainingHours is { } remaining && HoursInYear(request, schedule, year) > remaining) {
                exceeds = true;
            }
        }

        return (first, exceeds);
    }

    public async Task<HashSet<Guid>> DeductingHourTypeIdsAsync(CancellationToken cancellationToken) =>
        (await unitOfWork.HourTypes.GetAllAsync(includeInactive: true, cancellationToken))
        .Where(t => t.DeductsFromLeaveBalance)
        .Select(t => t.Id)
        .ToHashSet();

    public static decimal UsedHours(IEnumerable<TimeEntry> entries, IReadOnlySet<Guid> deductingTypeIds) =>
        Math.Round((decimal)entries.Where(e => deductingTypeIds.Contains(e.HourTypeId)).Sum(e => e.Duration.TotalHours), 2);

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;
    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
}

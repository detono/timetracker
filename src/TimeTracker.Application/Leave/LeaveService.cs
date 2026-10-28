using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Leave;

/// <summary>Where someone's working hours come from.</summary>
public enum WorkScheduleSource {
    /// <summary>Their own schedule.</summary>
    Personal,

    /// <summary>The company's default schedule.</summary>
    Company,

    /// <summary>Monday to Friday, 9:00 to 17:00: neither they nor the company set one.</summary>
    BuiltIn
}

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

    /// <summary>Someone's own schedule, else the company's, else the built-in Monday-to-Friday one.</summary>
    public async Task<WorkSchedule> GetScheduleAsync(Guid userId, CancellationToken cancellationToken) =>
        (await ResolveScheduleAsync(userId, cancellationToken)).Schedule;

    public async Task<(WorkSchedule Schedule, WorkScheduleSource Source)> ResolveScheduleAsync(
        Guid userId, CancellationToken cancellationToken
    ) {
        if (await unitOfWork.WorkSchedules.GetForUserAsync(userId, cancellationToken) is { } own) {
            return (own, WorkScheduleSource.Personal);
        }

        return await ResolveCompanyScheduleAsync(cancellationToken);
    }

    public async Task<(WorkSchedule Schedule, WorkScheduleSource Source)> ResolveCompanyScheduleAsync(
        CancellationToken cancellationToken
    ) =>
        await unitOfWork.WorkSchedules.GetCompanyDefaultAsync(cancellationToken) is { } company
            ? (company, WorkScheduleSource.Company)
            : (WorkSchedule.BuiltInDefault(), WorkScheduleSource.BuiltIn);

    public static decimal HoursOf(IEnumerable<LeaveDay> days) =>
        Math.Round((decimal)days.Sum(d => d.Duration.TotalHours), 2);

    /// <summary>The scheduled hours of <paramref name="request"/> that fall in <paramref name="year"/>.</summary>
    public static decimal HoursInYear(
        LeaveRequest request, WorkSchedule schedule, int year, IReadOnlySet<DateOnly>? holidays = null
    ) {
        if (request.StartDate.Year == year && request.EndDate.Year == year) {
            return request.Hours;
        }

        return HoursOf(LeavePlan.Plan(schedule, request.StartDate, request.EndDate,
                request.FirstDayPart, request.LastDayPart, holidays)
            .Where(d => d.Date.Year == year));
    }

    /// <summary>The company's public holidays and days off between the two dates, inclusive.</summary>
    public async Task<HashSet<DateOnly>> GetHolidaysAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        (await unitOfWork.PublicHolidays.GetBetweenAsync(from, to, cancellationToken)).Select(h => h.Date).ToHashSet();

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
        var holidays = await GetHolidaysAsync(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), cancellationToken);
        var pending = pendingRequests
            .Where(r => deducting.Contains(r.HourTypeId))
            .Sum(r => HoursInYear(r, schedule, year, holidays));

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
        var holidays = await GetHolidaysAsync(request.StartDate, request.EndDate, cancellationToken);
        for (var year = request.StartDate.Year; year <= request.EndDate.Year; year++) {
            var snapshot = await GetBalanceAsync(request.UserId, year, schedule, cancellationToken);
            first ??= snapshot;
            if (snapshot.RemainingHours is { } remaining && HoursInYear(request, schedule, year, holidays) > remaining) {
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
}

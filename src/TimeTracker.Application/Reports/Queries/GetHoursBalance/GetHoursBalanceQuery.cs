using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HoursOverview;
using TimeTracker.Application.Reports.Queries.GetHoursReport;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Reports.Queries.GetHoursBalance;

/// <summary>
/// Worked vs. expected hours per person per period, with the same rules as the hours overview:
/// expected hours count up to yesterday (today once it's logged), from the day someone
/// started, without public holidays. People who don't track hours are left out.
/// </summary>
public record GetHoursBalanceQuery(DateOnly From, DateOnly To, ReportGrouping Grouping, Guid? UserId = null)
    : IRequest<Result<IReadOnlyList<HoursBalanceLineDto>>>;

/// <param name="PeriodStart">The period's first day within the requested dates.</param>
/// <param name="PeriodEnd">The period's last day within the requested dates.</param>
/// <param name="DifferenceHours">Worked minus expected: negative means behind.</param>
public record HoursBalanceLineDto(
    Guid UserId,
    string UserFullName,
    string PeriodLabel,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal WorkedHours,
    decimal ExpectedHours,
    decimal DifferenceHours);

public class GetHoursBalanceQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    HoursOverviewLoader loader,
    IDateTimeProvider clock
) : IRequestHandler<GetHoursBalanceQuery, Result<IReadOnlyList<HoursBalanceLineDto>>> {
    public async Task<Result<IReadOnlyList<HoursBalanceLineDto>>> Handle(
        GetHoursBalanceQuery request, CancellationToken cancellationToken
    ) {
        if (request.To < request.From) {
            return Result<IReadOnlyList<HoursBalanceLineDto>>.Failure("'To' date must not be before 'From' date.");
        }

        var scope = await ReportScope.ResolveAsync(unitOfWork, currentUser, request.UserId, cancellationToken);
        if (!scope.Succeeded) {
            return Result<IReadOnlyList<HoursBalanceLineDto>>.Failure(scope.Error!, scope.ErrorType);
        }

        var users = scope.Value!.Where(u => u.TracksHours).ToList();
        if (users.Count == 0) {
            return Result<IReadOnlyList<HoursBalanceLineDto>>.Success(Array.Empty<HoursBalanceLineDto>());
        }

        var today = clock.TodayUtc;
        var entries = (await unitOfWork.TimeEntries.GetForUsersAsync(
                users.Select(u => u.Id).ToList(), request.From, request.To, cancellationToken))
            .ToLookup(e => e.UserId);
        var schedules = await loader.SchedulesAsync(users, cancellationToken);
        var holidays = (await unitOfWork.PublicHolidays.GetBetweenAsync(request.From, request.To, cancellationToken))
            .ToDictionary(h => h.Date, h => h.Name);
        var periods = ReportPeriods.Between(request.From, request.To, request.Grouping)
            .Where(p => p.Start <= today)
            .ToList();

        var lines = new List<HoursBalanceLineDto>();
        foreach (var user in users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)) {
            var userEntries = entries[user.Id].ToList();
            // Someone who left is only listed for the periods they were still around for.
            if (!user.IsActive && userEntries.Count == 0) {
                continue;
            }

            // Entries before the requested dates can't move the start into them, so these suffice.
            var countFrom = HoursOverviewCalculator.CountFrom(user, userEntries);
            foreach (var (start, end, label) in periods) {
                var from = start < request.From ? request.From : start;
                var to = end > request.To ? request.To : end;
                var period = HoursOverviewCalculator.Period(
                    from, to, today, countFrom, schedules[user.Id], userEntries, holidays);
                lines.Add(new HoursBalanceLineDto(
                    user.Id, $"{user.FirstName} {user.LastName}", label, from, to,
                    period.WorkedHours, period.ExpectedHours, period.DifferenceHours));
            }
        }

        return Result<IReadOnlyList<HoursBalanceLineDto>>.Success(
            lines.OrderBy(l => l.PeriodStart).ThenBy(l => l.UserFullName).ToList());
    }
}

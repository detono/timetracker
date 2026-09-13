using System.Globalization;
using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Reports.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Reports.Queries.GetHoursReport;

public class GetHoursReportQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<GetHoursReportQuery, Result<HoursReportDto>> {
    public async Task<Result<HoursReportDto>> Handle(GetHoursReportQuery request, CancellationToken cancellationToken) {
        if (request.To < request.From) {
            return Result<HoursReportDto>.Failure("'To' date must not be before 'From' date.");
        }

        IReadOnlyList<User> scopeUsers;

        if (currentUser.Role == UserRole.Employer) {
            scopeUsers = await unitOfWork.Users.GetAllAsync(cancellationToken);
        }
        else {
            var supervisees = await unitOfWork.Users.GetSupervisedByAsync(currentUser.UserId, cancellationToken);
            var self = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
            scopeUsers = self is null ? supervisees : supervisees.Append(self).ToList();

            if (scopeUsers.All(u => u.Id != currentUser.UserId) && request.UserId != currentUser.UserId) {
                return Result<HoursReportDto>.Failure(
                    "You do not have the authority to view this report.", ResultErrorType.Forbidden);
            }
        }

        if (request.UserId.HasValue) {
            if (scopeUsers.All(u => u.Id != request.UserId.Value)) {
                return Result<HoursReportDto>.Failure(
                    "You do not have the authority to view this employee's report.", ResultErrorType.Forbidden);
            }

            scopeUsers = scopeUsers.Where(u => u.Id == request.UserId.Value).ToList();
        }

        var userIds = scopeUsers.Select(u => u.Id).ToList();
        var userLookup = scopeUsers.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
        var hourTypes = (await unitOfWork.HourTypes.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(t => t.Id);

        var entries = userIds.Count == 0
            ? Array.Empty<TimeEntry>()
            : await unitOfWork.TimeEntries.GetForUsersAsync(userIds, request.From, request.To, cancellationToken);

        var lines = entries
            .GroupBy(e => new { e.UserId, e.HourTypeId, Period = GetPeriodKey(e.WorkDate, request.Grouping) })
            .Select(g => {
                var (start, end, label) = GetPeriodBounds(g.Key.Period, request.Grouping);
                hourTypes.TryGetValue(g.Key.HourTypeId, out var hourType);
                return new HoursReportLineDto(
                    g.Key.UserId,
                    userLookup.TryGetValue(g.Key.UserId, out var name) ? name : "Unknown",
                    g.Key.HourTypeId,
                    hourType?.LocalizedNames.GetValueOrDefault("en") ?? "Unknown",
                    hourType?.ColorHex ?? "#999999",
                    label,
                    start,
                    end,
                    Math.Round(g.Sum(e => e.Duration.TotalHours), 2),
                    g.Count());
            })
            .OrderBy(l => l.PeriodStart).ThenBy(l => l.UserFullName).ThenBy(l => l.HourTypeName)
            .ToList();

        var report = new HoursReportDto(
            request.Grouping.ToString(),
            request.From,
            request.To,
            lines,
            Math.Round(lines.Sum(l => l.TotalHours), 2));

        return Result<HoursReportDto>.Success(report);
    }

    private static string GetPeriodKey(DateOnly date, ReportGrouping grouping) => grouping switch {
        ReportGrouping.Day => date.ToString("yyyy-MM-dd"),
        ReportGrouping.Week =>
            $"{ISOWeek.GetYear(date.ToDateTime(TimeOnly.MinValue))}-W{ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue)):D2}",
        ReportGrouping.Month => date.ToString("yyyy-MM"),
        _ => throw new ArgumentOutOfRangeException(nameof(grouping))
    };

    private static (DateOnly Start, DateOnly End, string Label) GetPeriodBounds(string key, ReportGrouping grouping) {
        switch (grouping) {
            case ReportGrouping.Day:
                var day = DateOnly.ParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                return (day, day, day.ToString("yyyy-MM-dd"));

            case ReportGrouping.Week:
                var parts = key.Split("-W");
                var year = int.Parse(parts[0]);
                var week = int.Parse(parts[1]);
                var weekStart = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
                var weekEnd = weekStart.AddDays(6);
                return (DateOnly.FromDateTime(weekStart), DateOnly.FromDateTime(weekEnd), key);

            case ReportGrouping.Month:
                var monthStart = DateOnly.ParseExact(key + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                return (monthStart, monthEnd, monthStart.ToString("yyyy-MM"));

            default:
                throw new ArgumentOutOfRangeException(nameof(grouping));
        }
    }
}
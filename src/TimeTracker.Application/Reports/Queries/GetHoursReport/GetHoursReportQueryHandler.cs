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

        var scope = await ReportScope.ResolveAsync(unitOfWork, currentUser, request.UserId, cancellationToken);
        if (!scope.Succeeded) {
            return Result<HoursReportDto>.Failure(scope.Error!, scope.ErrorType);
        }

        var scopeUsers = scope.Value!;
        var userIds = scopeUsers.Select(u => u.Id).ToList();
        var userLookup = scopeUsers.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
        var hourTypes = (await unitOfWork.HourTypes.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(t => t.Id);

        // Hour type names in the caller's language (e.g. in the CSV), else English, else any.
        var language = (await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken))?.Language ?? "en";
        string TypeName(HourType? type) =>
            type is null ? "Unknown"
            : type.LocalizedNames.TryGetValue(language, out var name) ? name
            : type.LocalizedNames.TryGetValue("en", out var en) ? en
            : type.LocalizedNames.Values.FirstOrDefault() ?? "Unknown";

        var entries = userIds.Count == 0
            ? Array.Empty<TimeEntry>()
            : await unitOfWork.TimeEntries.GetForUsersAsync(userIds, request.From, request.To, cancellationToken);

        var lines = entries
            .GroupBy(e => new { e.UserId, e.HourTypeId, Period = ReportPeriods.Key(e.WorkDate, request.Grouping) })
            .Select(g => {
                var (start, end, label) = ReportPeriods.Bounds(g.Key.Period, request.Grouping);
                hourTypes.TryGetValue(g.Key.HourTypeId, out var hourType);
                return new HoursReportLineDto(
                    g.Key.UserId,
                    userLookup.TryGetValue(g.Key.UserId, out var name) ? name : "Unknown",
                    g.Key.HourTypeId,
                    TypeName(hourType),
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
}

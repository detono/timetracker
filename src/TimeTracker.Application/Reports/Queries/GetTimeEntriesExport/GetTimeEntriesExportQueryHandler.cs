using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Reports.Queries.GetTimeEntriesExport;

public class GetTimeEntriesExportQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<GetTimeEntriesExportQuery, Result<IReadOnlyList<TimeEntryExportLineDto>>> {
    public async Task<Result<IReadOnlyList<TimeEntryExportLineDto>>> Handle(
        GetTimeEntriesExportQuery request, CancellationToken cancellationToken
    ) {
        if (request.To < request.From) {
            return Result<IReadOnlyList<TimeEntryExportLineDto>>.Failure("'To' date must not be before 'From' date.");
        }

        var scope = await ReportScope.ResolveAsync(unitOfWork, currentUser, request.UserId, cancellationToken);
        if (!scope.Succeeded) {
            return Result<IReadOnlyList<TimeEntryExportLineDto>>.Failure(scope.Error!, scope.ErrorType);
        }

        var names = scope.Value!.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
        if (names.Count == 0) {
            return Result<IReadOnlyList<TimeEntryExportLineDto>>.Success(Array.Empty<TimeEntryExportLineDto>());
        }

        var entries = await unitOfWork.TimeEntries.GetForUsersAsync(
            names.Keys.ToList(), request.From, request.To, cancellationToken);
        var hourTypes = (await unitOfWork.HourTypes.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(t => t.Id);
        var projects = (await unitOfWork.Projects.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(p => p.Id, p => p.Name);
        var language = (await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken))?.Language ?? "en";

        IReadOnlyList<TimeEntryExportLineDto> lines = entries
            .Select(e => new TimeEntryExportLineDto(
                e.UserId,
                names.TryGetValue(e.UserId, out var name) ? name : "Unknown",
                e.WorkDate,
                e.StartTime,
                e.EndTime,
                e.BreakMinutes,
                Math.Round(e.Duration.TotalHours, 2),
                HourTypeNames.For(hourTypes.GetValueOrDefault(e.HourTypeId), language),
                e.ProjectId is { } projectId ? projects.GetValueOrDefault(projectId) : null,
                e.Location,
                e.Notes,
                e.CreatedAtUtc,
                e.UpdatedAtUtc))
            .OrderBy(l => l.WorkDate).ThenBy(l => l.UserFullName).ThenBy(l => l.StartTime)
            .ToList();

        return Result<IReadOnlyList<TimeEntryExportLineDto>>.Success(lines);
    }
}

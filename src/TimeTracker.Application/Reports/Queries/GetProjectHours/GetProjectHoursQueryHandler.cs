using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Reports.Queries.GetProjectHours;

public class GetProjectHoursQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<GetProjectHoursQuery, Result<IReadOnlyList<ProjectHoursLineDto>>> {
    public async Task<Result<IReadOnlyList<ProjectHoursLineDto>>> Handle(
        GetProjectHoursQuery request, CancellationToken cancellationToken
    ) {
        if (request.To < request.From) {
            return Result<IReadOnlyList<ProjectHoursLineDto>>.Failure("'To' date must not be before 'From' date.");
        }

        var scope = await ReportScope.ResolveAsync(unitOfWork, currentUser, request.UserId, cancellationToken);
        if (!scope.Succeeded) {
            return Result<IReadOnlyList<ProjectHoursLineDto>>.Failure(scope.Error!, scope.ErrorType);
        }

        var userIds = scope.Value!.Select(u => u.Id).ToList();
        if (userIds.Count == 0) {
            return Result<IReadOnlyList<ProjectHoursLineDto>>.Success(Array.Empty<ProjectHoursLineDto>());
        }

        var entries = await unitOfWork.TimeEntries.GetForUsersAsync(userIds, request.From, request.To, cancellationToken);
        var projects = (await unitOfWork.Projects.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(p => p.Id);

        IReadOnlyList<ProjectHoursLineDto> lines = entries
            .GroupBy(e => e.ProjectId)
            .Select(g => {
                var project = g.Key is { } id ? projects.GetValueOrDefault(id) : null;
                return new ProjectHoursLineDto(
                    project?.Id,
                    project?.Name,
                    project?.ClientName,
                    project?.IsBillable ?? false,
                    Math.Round(g.Sum(e => e.Duration.TotalHours), 2),
                    g.Count());
            })
            // Projects by client and name; hours without a project last.
            .OrderBy(l => l.ProjectId is null)
            .ThenBy(l => l.ClientName ?? "", StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.ProjectName ?? "", StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<IReadOnlyList<ProjectHoursLineDto>>.Success(lines);
    }
}

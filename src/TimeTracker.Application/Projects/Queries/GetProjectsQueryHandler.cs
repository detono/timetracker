using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Projects.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects.Queries;

public class GetProjectsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService
) : IRequestHandler<GetProjectsQuery, Result<IReadOnlyList<ProjectDto>>> {
    public async Task<Result<IReadOnlyList<ProjectDto>>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken
    ) {
        var isEmployer = currentUserService.Role == UserRole.Employer;
        var includeInactive = request.IncludeInactive && isEmployer;

        var projects = await unitOfWork.Projects.GetAllAsync(
            includeInactive,
            cancellationToken
        );

        var projectEntries = await unitOfWork.TimeEntries.GetAllWithProjectIdAsync(cancellationToken);

        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        var dtos = projects.Select(p => {
            var entriesForProject = projectEntries.Where(e => e.ProjectId == p.Id).ToList();

            var lifetime = entriesForProject.Sum(e => e.Duration.TotalHours);
            var thisMonth = entriesForProject
                .Where(e => e.WorkDate.Month == currentMonth && e.WorkDate.Year == currentYear)
                .Sum(e => e.Duration.TotalHours);

            return new ProjectDto(
                p.Id,
                p.Name,
                p.ClientName,
                p.IsActive,
                Math.Round(lifetime, 2),
                Math.Round(thisMonth, 2)
            );
        }).ToList();

        return Result<IReadOnlyList<ProjectDto>>.Success(dtos);
    }
}
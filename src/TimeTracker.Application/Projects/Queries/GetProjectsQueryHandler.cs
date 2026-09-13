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

        var dtos = projects
            .Select(p => new ProjectDto(p.Id, p.Name, p.ClientName, p.IsActive))
            .ToList();

        return Result<IReadOnlyList<ProjectDto>>.Success(dtos);
    }
}
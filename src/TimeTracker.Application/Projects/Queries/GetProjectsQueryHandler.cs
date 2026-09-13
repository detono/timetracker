using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Projects.Dtos;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects.Queries;

public class GetProjectsQueryHandler(
    IUnitOfWork unitOfWork
) : IRequestHandler<GetProjectsQuery, Result<IReadOnlyList<ProjectDto>>> {
    public async Task<Result<IReadOnlyList<ProjectDto>>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken
    ) {
        var projects = await unitOfWork.Projects.GetAllAsync(
            request.IncludeInactive, 
            cancellationToken
        );

        var dtos = projects
            .Select(p => new ProjectDto(p.Id, p.Name, p.ClientName, p.IsActive))
            .ToList();

        return Result<IReadOnlyList<ProjectDto>>.Success(dtos);
    }
}
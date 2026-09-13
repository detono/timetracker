using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler(
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateProjectCommand, Result<Guid>> {
    public async Task<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken) {
        var project = Project.Create(request.Name, request.ClientName);

        await unitOfWork.Projects.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(project.Id);
    }
}
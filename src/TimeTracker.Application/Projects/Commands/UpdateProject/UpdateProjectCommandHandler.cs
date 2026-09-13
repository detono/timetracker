using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandler(
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateProjectCommand, Result> {
    public async Task<Result> Handle(UpdateProjectCommand request, CancellationToken cancellationToken) {
        var project = await unitOfWork.Projects.GetByIdAsync(request.Id, cancellationToken);
        if (project is null) {
            return Result.Failure("Project was not found.", ResultErrorType.NotFound);
        }

        project.Update(request.Name, request.ClientName);

        unitOfWork.Projects.Update(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
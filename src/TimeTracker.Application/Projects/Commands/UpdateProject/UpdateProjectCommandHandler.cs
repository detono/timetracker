using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService
) : IRequestHandler<UpdateProjectCommand, Result> {
    public async Task<Result> Handle(UpdateProjectCommand request, CancellationToken cancellationToken) {
        if (currentUserService.Role != UserRole.Employer) {
            return Result<Guid>.Failure("Only employers can manage projects.", ResultErrorType.Forbidden);
        }
        
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
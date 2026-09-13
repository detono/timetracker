using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects.Commands.ToggleProjectStatus;

public class ToggleProjectStatusCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService
) : IRequestHandler<ToggleProjectStatusCommand, Result> {
    public async Task<Result> Handle(ToggleProjectStatusCommand request, CancellationToken cancellationToken) {
        if (currentUserService.Role != UserRole.Employer) {
            return Result<Guid>.Failure("Only employers can manage projects.", ResultErrorType.Forbidden);
        }
        
        var project = await unitOfWork.Projects.GetByIdAsync(request.Id, cancellationToken);
        if (project is null) {
            return Result.Failure("Project was not found.", ResultErrorType.NotFound);
        }

        if (request.IsActive) project.Activate();
        else project.Deactivate();

        unitOfWork.Projects.Update(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
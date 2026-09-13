using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects.Commands.ToggleProjectStatus;

public class ToggleProjectStatusCommandHandler(
    IUnitOfWork unitOfWork
) : IRequestHandler<ToggleProjectStatusCommand, Result> {
    public async Task<Result> Handle(ToggleProjectStatusCommand request, CancellationToken cancellationToken) {
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
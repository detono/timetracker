using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService 
) : IRequestHandler<CreateProjectCommand, Result<Guid>> {
    public async Task<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken) {
        if (currentUserService.Role != UserRole.Employer) {
            return Result<Guid>.Failure("Only employers can manage projects.", ResultErrorType.Forbidden);
        }
        
        var project = Project.Create(request.Name, request.ClientName);

        await unitOfWork.Projects.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(project.Id);
    }
}
using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.PlatformAdmins.Commands.SetPlatformAdminActive;

public class SetPlatformAdminActiveCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<SetPlatformAdminActiveCommand, Result> {
    public async Task<Result> Handle(SetPlatformAdminActiveCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.PlatformAdmin) {
            return Result.Failure("Only a platform admin can change a platform admin's status.",
                ResultErrorType.Forbidden);
        }

        // Also guarantees at least one active admin remains, since the caller is one.
        if (!request.IsActive && request.Id == currentUser.UserId) {
            return Result.Failure("You cannot deactivate your own account.", ResultErrorType.Validation);
        }

        var admin = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);
        if (admin is null || admin.Role != UserRole.PlatformAdmin) {
            return Result.Failure("Platform admin was not found.", ResultErrorType.NotFound);
        }

        if (request.IsActive) {
            admin.Activate();
        } else {
            admin.Deactivate();
        }

        unitOfWork.Users.Update(admin);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

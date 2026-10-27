using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Tenants.Commands.SetTenantActive;

public class SetTenantActiveCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<SetTenantActiveCommand, Result> {
    public async Task<Result> Handle(SetTenantActiveCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.PlatformAdmin) {
            return Result.Failure("Only a platform admin can change a tenant's status.", ResultErrorType.Forbidden);
        }

        var tenant = await unitOfWork.Tenants.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null) {
            return Result.Failure("Tenant was not found.", ResultErrorType.NotFound);
        }

        if (request.IsActive) {
            tenant.Activate();
        } else {
            tenant.Deactivate();
        }

        unitOfWork.Tenants.Update(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

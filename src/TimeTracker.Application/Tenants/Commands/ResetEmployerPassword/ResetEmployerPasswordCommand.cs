using FluentValidation;
using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Tenants.Commands.ResetEmployerPassword;

/// <summary>
/// Platform admin only: sets a temporary password for one of a company's employers, e.g. when
/// the only employer is locked out. They're signed out everywhere and must choose their own
/// password at their next login. Only employer accounts; employers reset their employees.
/// </summary>
public record ResetEmployerPasswordCommand(Guid TenantId, Guid UserId, string NewPassword) : IRequest<Result>;

public class ResetEmployerPasswordCommandValidator : AbstractValidator<ResetEmployerPasswordCommand> {
    public ResetEmployerPasswordCommandValidator() {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .WithMessage("New password must be at least 8 characters long.");
    }
}

public class ResetEmployerPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ITenantScope tenantScope,
    IPasswordHasher passwordHasher
) : IRequestHandler<ResetEmployerPasswordCommand, Result> {
    public async Task<Result> Handle(ResetEmployerPasswordCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.PlatformAdmin) {
            return Result.Failure("Only a platform admin can reset an employer's password.", ResultErrorType.Forbidden);
        }

        if (await unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken) is null) {
            return Result.Failure("Company was not found.", ResultErrorType.NotFound);
        }

        using var scope = tenantScope.Enter(request.TenantId);

        // Only finds users of this company, thanks to the scope.
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is not { Role: UserRole.Employer }) {
            return Result.Failure("Employer was not found in this company.", ResultErrorType.NotFound);
        }

        user.SetTemporaryPassword(passwordHasher.Hash(request.NewPassword));
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using MediatR;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher,
    ITokenService tokenService
) : IRequestHandler<ChangePasswordCommand, Result<AuthResultDto>> {
    public async Task<Result<AuthResultDto>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken) {
        var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null) {
            return Result<AuthResultDto>.Failure("User was not found.", ResultErrorType.NotFound);
        }

        // Someone who just signed in with a temporary password already proved they know it.
        if (!user.MustChangePassword &&
            (string.IsNullOrEmpty(request.CurrentPassword) || !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))) {
            return Result<AuthResultDto>.Failure("Current password is incorrect.", ResultErrorType.Validation);
        }

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash)) {
            return Result<AuthResultDto>.Failure(
                "New password must be different from the current password.", ResultErrorType.Validation);
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tenant = user.TenantId is { } tenantId ? await unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken) : null;
        return Result<AuthResultDto>.Success(AuthResults.Create(user, tenant?.Name, tokenService));
    }
}

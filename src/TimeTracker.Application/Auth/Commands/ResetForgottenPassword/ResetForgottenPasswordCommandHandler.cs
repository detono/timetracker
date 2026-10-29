using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Auth.Commands.ResetForgottenPassword;

public class ResetForgottenPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    ITenantScope tenantScope,
    IPasswordHasher passwordHasher,
    IDateTimeProvider clock,
    ILoginThrottle loginThrottle
) : IRequestHandler<ResetForgottenPasswordCommand, Result> {
    // The same message for every failure, so it doesn't reveal which accounts exist.
    public const string InvalidCodeMessage = "This code is wrong or has expired. Request a new one.";

    public async Task<Result> Handle(ResetForgottenPasswordCommand request, CancellationToken cancellationToken) {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is not { IsActive: true, TenantId: { } tenantId }) {
            return Invalid();
        }

        var tenant = await unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is not { IsActive: true }) {
            return Invalid();
        }

        using var scope = tenantScope.Enter(tenantId);

        var code = await unitOfWork.PasswordResetCodes.GetLatestForUserAsync(user.Id, cancellationToken);
        if (code is null || !code.IsUsable(clock.UtcNow)) {
            return Invalid();
        }

        if (!passwordHasher.Verify(PasswordResetCodes.Normalize(request.Code), code.CodeHash)) {
            code.RecordFailedAttempt();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Invalid();
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        unitOfWork.Users.Update(user);
        await unitOfWork.PasswordResetCodes.RemoveAllForUserAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Someone locked out by wrong guesses can sign in again with their new password.
        loginThrottle.Reset(user.Email);
        return Result.Success();
    }

    private static Result Invalid() => Result.Failure(InvalidCodeMessage, ResultErrorType.Validation);
}

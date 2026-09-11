using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Users.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher
) : IRequestHandler<ResetPasswordCommand, Result> {
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer)
            return Result.Failure("Only an employer can reset another user's password.", ResultErrorType.Forbidden);

        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure("User was not found.", ResultErrorType.NotFound);

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
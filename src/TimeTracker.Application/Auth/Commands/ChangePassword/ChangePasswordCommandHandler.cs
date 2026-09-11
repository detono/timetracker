using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher
) : IRequestHandler<ChangePasswordCommand, Result> {
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken) {
        var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
            return Result.Failure("User was not found.", ResultErrorType.NotFound);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("Current password is incorrect.", ResultErrorType.Validation);

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
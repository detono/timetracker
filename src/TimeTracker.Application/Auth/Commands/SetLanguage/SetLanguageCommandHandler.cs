using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Auth.Commands.SetLanguage;

public class SetLanguageCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<SetLanguageCommand, Result> {
    public async Task<Result> Handle(SetLanguageCommand request, CancellationToken cancellationToken) {
        var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null) {
            return Result.Failure("Your account was not found.", ResultErrorType.NotFound);
        }

        user.SetLanguage(request.Language);
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

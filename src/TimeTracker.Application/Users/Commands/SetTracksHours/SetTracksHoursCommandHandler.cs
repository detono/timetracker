using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Users.Commands.SetTracksHours;

public class SetTracksHoursCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<SetTracksHoursCommand, Result> {
    public async Task<Result> Handle(SetTracksHoursCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result.Failure("Only an employer can change this.", ResultErrorType.Forbidden);
        }

        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) {
            return Result.Failure("User was not found.", ResultErrorType.NotFound);
        }

        user.SetTracksHours(request.TracksHours);
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

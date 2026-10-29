using MediatR;
using TimeTracker.Application.AuditLog;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Users.Commands.DeactivateUser;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly AuditTrail _audit;

    public DeactivateUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, AuditTrail audit)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Employer)
        {
            return Result.Failure("Only an employer can deactivate a user.", ResultErrorType.Forbidden);
        }

        if (request.UserId == _currentUser.UserId)
        {
            return Result.Failure("You cannot deactivate your own account.", ResultErrorType.Validation);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure("User was not found.", ResultErrorType.NotFound);
        }

        user.Deactivate();
        _unitOfWork.Users.Update(user);
        await _audit.RecordAsync(AuditAction.UserDeactivated, user, null, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

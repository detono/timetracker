using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LeaveBalances.Commands.SetLeaveAllowance;

public class SetLeaveAllowanceCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<SetLeaveAllowanceCommand, Result> {
    public async Task<Result> Handle(SetLeaveAllowanceCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result.Failure("Only an employer can set leave allowances.", ResultErrorType.Forbidden);
        }

        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) {
            return Result.Failure("User was not found.", ResultErrorType.NotFound);
        }

        var balance = await unitOfWork.LeaveBalances.GetAsync(user.Id, request.Year, cancellationToken);
        if (balance is null) {
            await unitOfWork.LeaveBalances.AddAsync(
                LeaveBalance.Create(user.Id, request.Year, request.AllowanceHours), cancellationToken);
        } else {
            balance.SetAllowance(request.AllowanceHours);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

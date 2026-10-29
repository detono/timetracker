using MediatR;
using TimeTracker.Application.AuditLog;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.LockedMonths;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Commands.DeleteTimeEntry;

public class DeleteTimeEntryCommandHandler : IRequestHandler<DeleteTimeEntryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly MonthLocks _monthLocks;
    private readonly AuditTrail _audit;

    public DeleteTimeEntryCommandHandler(
        IUnitOfWork unitOfWork, ICurrentUserService currentUser, MonthLocks monthLocks, AuditTrail audit)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _monthLocks = monthLocks;
        _audit = audit;
    }

    public async Task<Result> Handle(DeleteTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _unitOfWork.TimeEntries.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
        {
            return Result.Failure("Time entry was not found.", ResultErrorType.NotFound);
        }

        if (entry.UserId != _currentUser.UserId && _currentUser.Role != UserRole.Employer)
        {
            return Result.Failure("You are not allowed to delete this time entry.", ResultErrorType.Forbidden);
        }

        if (await _monthLocks.FindLockedAsync(entry.WorkDate, cancellationToken) is { } locked)
        {
            return Result.Failure(locked, ResultErrorType.Conflict);
        }

        if (entry.UserId != _currentUser.UserId)
        {
            var owner = await _unitOfWork.Users.GetByIdAsync(entry.UserId, cancellationToken);
            var hourType = await _unitOfWork.HourTypes.GetByIdAsync(entry.HourTypeId, cancellationToken);
            await _audit.RecordAsync(AuditAction.TimeEntryDeleted, owner,
                new Dictionary<string, string> { ["entry"] = AuditTrail.Describe(entry, hourType) }, cancellationToken);
        }

        _unitOfWork.TimeEntries.Remove(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Commands.DeleteTimeEntry;

public class DeleteTimeEntryCommandHandler : IRequestHandler<DeleteTimeEntryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteTimeEntryCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
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

        _unitOfWork.TimeEntries.Remove(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Commands.CreateTimeEntry;

public class CreateTimeEntryCommandHandler : IRequestHandler<CreateTimeEntryCommand, Result<TimeEntryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateTimeEntryCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<TimeEntryDto>> Handle(CreateTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var targetUserId = request.TargetUserId ?? _currentUser.UserId;

        // Only an Employer may log time on behalf of somebody else.
        if (targetUserId != _currentUser.UserId && _currentUser.Role != UserRole.Employer)
        {
            return Result<TimeEntryDto>.Failure(
                "You are not allowed to log time on behalf of another employee.",
                ResultErrorType.Forbidden);
        }

        var targetUser = await _unitOfWork.Users.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser is null)
        {
            return Result<TimeEntryDto>.Failure("Target user was not found.", ResultErrorType.NotFound);
        }

        var hourType = await _unitOfWork.HourTypes.GetByIdAsync(request.HourTypeId, cancellationToken);
        if (hourType is null || !hourType.IsActive)
        {
            return Result<TimeEntryDto>.Failure("Hour type was not found or is no longer active.", ResultErrorType.Validation);
        }

        var entry = TimeEntry.Create(
            targetUserId,
            request.HourTypeId,
            request.WorkDate,
            request.StartTime,
            request.EndTime,
            request.BreakMinutes,
            request.Notes);

        await _unitOfWork.TimeEntries.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new TimeEntryDto(
            entry.Id,
            entry.UserId,
            $"{targetUser.FirstName} {targetUser.LastName}",
            hourType.Id,
            hourType.Name,
            hourType.ColorHex,
            entry.WorkDate,
            entry.StartTime,
            entry.EndTime,
            entry.BreakMinutes,
            Math.Round(entry.Duration.TotalHours, 2),
            entry.Notes);

        return Result<TimeEntryDto>.Success(dto);
    }
}

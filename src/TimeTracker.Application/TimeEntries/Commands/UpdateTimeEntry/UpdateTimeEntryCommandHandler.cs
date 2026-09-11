using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Commands.UpdateTimeEntry;

public class UpdateTimeEntryCommandHandler : IRequestHandler<UpdateTimeEntryCommand, Result<TimeEntryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateTimeEntryCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<TimeEntryDto>> Handle(UpdateTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _unitOfWork.TimeEntries.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
        {
            return Result<TimeEntryDto>.Failure("Time entry was not found.", ResultErrorType.NotFound);
        }

        if (entry.UserId != _currentUser.UserId && _currentUser.Role != UserRole.Employer)
        {
            return Result<TimeEntryDto>.Failure("You are not allowed to edit this time entry.", ResultErrorType.Forbidden);
        }

        var hourType = await _unitOfWork.HourTypes.GetByIdAsync(request.HourTypeId, cancellationToken);
        if (hourType is null || !hourType.IsActive)
        {
            return Result<TimeEntryDto>.Failure("Hour type was not found or is no longer active.", ResultErrorType.Validation);
        }

        entry.Reschedule(request.WorkDate);
        entry.SetTimes(request.StartTime, request.EndTime, request.BreakMinutes);
        entry.SetHourType(request.HourTypeId);
        entry.UpdateNotes(request.Notes);

        _unitOfWork.TimeEntries.Update(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await _unitOfWork.Users.GetByIdAsync(entry.UserId, cancellationToken);

        var dto = new TimeEntryDto(
            entry.Id,
            entry.UserId,
            user is null ? string.Empty : $"{user.FirstName} {user.LastName}",
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

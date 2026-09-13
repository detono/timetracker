using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Commands.UpdateTimeEntry;

public class UpdateTimeEntryCommandHandler(
    IUnitOfWork unitOfWork, 
    ICurrentUserService currentUser
) : IRequestHandler<UpdateTimeEntryCommand, Result<TimeEntryDto>> {
    public async Task<Result<TimeEntryDto>>
        Handle(UpdateTimeEntryCommand request, CancellationToken cancellationToken) {
        var entry = await unitOfWork.TimeEntries.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null) {
            return Result<TimeEntryDto>.Failure("Time entry was not found.", ResultErrorType.NotFound);
        }

        if (entry.UserId != currentUser.UserId && currentUser.Role != UserRole.Employer) {
            return Result<TimeEntryDto>.Failure("You are not allowed to edit this time entry.",
                ResultErrorType.Forbidden);
        }

        var hourType = await unitOfWork.HourTypes.GetByIdAsync(request.HourTypeId, cancellationToken);
        if (hourType is null || !hourType.IsActive) {
            return Result<TimeEntryDto>.Failure("Hour type was not found or is no longer active.",
                ResultErrorType.Validation);
        }

        entry.Reschedule(request.WorkDate);
        entry.SetTimes(request.StartTime, request.EndTime, request.BreakMinutes);
        entry.SetHourType(request.HourTypeId);
        entry.UpdateNotes(request.Notes);
        entry.UpdateProjectId(request.ProjectId);
        
        string? projectName = null;
        if (request.ProjectId.HasValue) { 
            var project = await unitOfWork.Projects.GetByIdAsync(request.ProjectId.Value, cancellationToken);
            projectName = project?.Name; 
        }
        
        unitOfWork.TimeEntries.Update(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await unitOfWork.Users.GetByIdAsync(entry.UserId, cancellationToken);

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
            entry.Notes,
            entry.ProjectId,
            projectName
        );

        return Result<TimeEntryDto>.Success(dto);
    }
}
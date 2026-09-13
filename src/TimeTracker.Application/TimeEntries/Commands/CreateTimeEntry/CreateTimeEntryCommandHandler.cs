using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Commands.CreateTimeEntry;

public class CreateTimeEntryCommandHandler(
    IUnitOfWork unitOfWork, 
    ICurrentUserService currentUser
) : IRequestHandler<CreateTimeEntryCommand, Result<TimeEntryDto>> {
    public async Task<Result<TimeEntryDto>>
        Handle(CreateTimeEntryCommand request, CancellationToken cancellationToken) {
        var targetUserId = request.TargetUserId ?? currentUser.UserId;

        // Only an Employer may log time on behalf of somebody else.
        if (targetUserId != currentUser.UserId && currentUser.Role != UserRole.Employer) {
            return Result<TimeEntryDto>.Failure(
                "You are not allowed to log time on behalf of another employee.",
                ResultErrorType.Forbidden);
        }

        var targetUser = await unitOfWork.Users.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser is null) {
            return Result<TimeEntryDto>.Failure("Target user was not found.", ResultErrorType.NotFound);
        }

        var hourType = await unitOfWork.HourTypes.GetByIdAsync(request.HourTypeId, cancellationToken);
        if (hourType is null || !hourType.IsActive) {
            return Result<TimeEntryDto>.Failure(
                "Hour type was not found or is no longer active.",
                ResultErrorType.Validation
            );
        }

        string? projectName = null;
        if (request.ProjectId.HasValue) { 
            var project = await unitOfWork.Projects.GetByIdAsync(request.ProjectId.Value, cancellationToken);
            projectName = project?.Name; 
        }
        

        var entry = TimeEntry.Create(
            userId: targetUserId,
            hourTypeId: request.HourTypeId,
            workDate: request.WorkDate,
            startTime: request.StartTime,
            endTime: request.EndTime,
            breakMinutes: request.BreakMinutes,
            notes: request.Notes,
            projectId: request.ProjectId
        );

        await unitOfWork.TimeEntries.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new TimeEntryDto(
            entry.Id,
            entry.UserId,
            $"{targetUser.FirstName} {targetUser.LastName}",
            hourType.Id,
            hourType.LocalizedNames, 
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
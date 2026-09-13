using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Queries.GetTimeEntries;

public class GetTimeEntriesQueryHandler(
    IUnitOfWork unitOfWork, 
    ICurrentUserService currentUser
) : IRequestHandler<GetTimeEntriesQuery, Result<IReadOnlyList<TimeEntryDto>>> {
    public async Task<Result<IReadOnlyList<TimeEntryDto>>> Handle(
        GetTimeEntriesQuery request,
        CancellationToken cancellationToken
    ) {
        var requestingUser = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (requestingUser is null) {
            return Result<IReadOnlyList<TimeEntryDto>>.Failure("Requesting user was not found.",
                ResultErrorType.NotFound);
        }

        var targetUser = await unitOfWork.Users.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (targetUser is null) {
            return Result<IReadOnlyList<TimeEntryDto>>.Failure("Target user was not found.", ResultErrorType.NotFound);
        }

        if (!requestingUser.CanViewHoursOf(targetUser.Id, targetUser.SupervisorId)) {
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(
                "You do not have the authority to view this employee's hours.",
                ResultErrorType.Forbidden);
        }

        var entries = await unitOfWork.TimeEntries.GetForUserAsync(
            targetUser.Id, request.From, request.To, cancellationToken);

        // Includes inactive types too, so historical entries still display their type's
        // name/color correctly even after an employer retires that type.
        var hourTypes = (await unitOfWork.HourTypes.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(t => t.Id);

        var projects = (await unitOfWork.Projects.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(p => p.Id);
        
        var dtos = entries
            .OrderBy(e => e.WorkDate).ThenBy(e => e.StartTime)
            .Select(e => {
                hourTypes.TryGetValue(e.HourTypeId, out var hourType);

                Project? project = null;
                if (e.ProjectId.HasValue) {
                    projects.TryGetValue(e.ProjectId.Value, out project);
                }

                return new TimeEntryDto(
                    e.Id,
                    e.UserId,
                    $"{targetUser.FirstName} {targetUser.LastName}",
                    e.HourTypeId,
                    hourType?.Name ?? "Unknown",
                    hourType?.ColorHex ?? "#999999",
                    e.WorkDate,
                    e.StartTime,
                    e.EndTime,
                    e.BreakMinutes,
                    Math.Round(e.Duration.TotalHours, 2),
                    e.Notes,
                    e.ProjectId,
                    project?.Name
                );
            })
            .ToList();

        return Result<IReadOnlyList<TimeEntryDto>>.Success(dtos);
    }
}
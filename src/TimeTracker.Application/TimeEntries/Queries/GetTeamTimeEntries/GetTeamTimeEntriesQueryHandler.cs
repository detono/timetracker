using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Queries.GetTeamTimeEntries;

public class GetTeamTimeEntriesQueryHandler(
    IUnitOfWork unitOfWork, 
    ICurrentUserService currentUser
) : IRequestHandler<GetTeamTimeEntriesQuery, Result<IReadOnlyList<TimeEntryDto>>> {
    public async Task<Result<IReadOnlyList<TimeEntryDto>>> Handle(
        GetTeamTimeEntriesQuery request,
        CancellationToken cancellationToken
    ) {
        IReadOnlyList<Domain.Entities.TimeEntry> entries;
        IReadOnlyList<Domain.Entities.User> users;

        if (currentUser.Role == UserRole.Employer) {
            users = await unitOfWork.Users.GetAllAsync(cancellationToken);
            entries = await unitOfWork.TimeEntries.GetAllAsync(request.From, request.To, cancellationToken);
        }
        else {
            var supervisees = await unitOfWork.Users.GetSupervisedByAsync(currentUser.UserId, cancellationToken);
            var self = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);

            users = self is null ? supervisees : supervisees.Append(self).ToList();

            if (users.Count == 0) {
                return Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());
            }

            entries = await unitOfWork.TimeEntries.GetForUsersAsync(
                users.Select(u => u.Id).ToList(), request.From, request.To, cancellationToken);
        }

        var userLookup = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
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
                    userLookup.TryGetValue(e.UserId, out var name) ? name : "Unknown",
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
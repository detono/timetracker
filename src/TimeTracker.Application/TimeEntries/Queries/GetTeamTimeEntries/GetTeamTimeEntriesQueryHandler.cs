using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Queries.GetTeamTimeEntries;

public class GetTeamTimeEntriesQueryHandler : IRequestHandler<GetTeamTimeEntriesQuery, Result<IReadOnlyList<TimeEntryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetTeamTimeEntriesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TimeEntryDto>>> Handle(GetTeamTimeEntriesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.TimeEntry> entries;
        IReadOnlyList<Domain.Entities.User> users;

        if (_currentUser.Role == UserRole.Employer)
        {
            users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            entries = await _unitOfWork.TimeEntries.GetAllAsync(request.From, request.To, cancellationToken);
        }
        else
        {
            var supervisees = await _unitOfWork.Users.GetSupervisedByAsync(_currentUser.UserId, cancellationToken);
            var self = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId, cancellationToken);

            users = self is null ? supervisees : supervisees.Append(self).ToList();

            if (users.Count == 0)
            {
                return Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());
            }

            entries = await _unitOfWork.TimeEntries.GetForUsersAsync(
                users.Select(u => u.Id).ToList(), request.From, request.To, cancellationToken);
        }

        var userLookup = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
        var hourTypes = (await _unitOfWork.HourTypes.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(t => t.Id);

        var dtos = entries
            .OrderBy(e => e.WorkDate).ThenBy(e => e.StartTime)
            .Select(e =>
            {
                hourTypes.TryGetValue(e.HourTypeId, out var hourType);
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
                    e.Notes);
            })
            .ToList();

        return Result<IReadOnlyList<TimeEntryDto>>.Success(dtos);
    }
}

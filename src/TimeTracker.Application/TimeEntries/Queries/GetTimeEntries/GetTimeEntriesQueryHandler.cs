using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.TimeEntries.Queries.GetTimeEntries;

public class GetTimeEntriesQueryHandler : IRequestHandler<GetTimeEntriesQuery, Result<IReadOnlyList<TimeEntryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetTimeEntriesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TimeEntryDto>>> Handle(GetTimeEntriesQuery request, CancellationToken cancellationToken)
    {
        var requestingUser = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (requestingUser is null)
        {
            return Result<IReadOnlyList<TimeEntryDto>>.Failure("Requesting user was not found.", ResultErrorType.NotFound);
        }

        var targetUser = await _unitOfWork.Users.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (targetUser is null)
        {
            return Result<IReadOnlyList<TimeEntryDto>>.Failure("Target user was not found.", ResultErrorType.NotFound);
        }

        if (!requestingUser.CanViewHoursOf(targetUser.Id, targetUser.SupervisorId))
        {
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(
                "You do not have the authority to view this employee's hours.",
                ResultErrorType.Forbidden);
        }

        var entries = await _unitOfWork.TimeEntries.GetForUserAsync(
            targetUser.Id, request.From, request.To, cancellationToken);

        // Includes inactive types too, so historical entries still display their type's
        // name/color correctly even after an employer retires that type.
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
                    $"{targetUser.FirstName} {targetUser.LastName}",
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

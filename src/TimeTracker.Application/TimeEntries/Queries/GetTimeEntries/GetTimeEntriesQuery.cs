using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;

namespace TimeTracker.Application.TimeEntries.Queries.GetTimeEntries;

/// <summary>
/// Retrieves time entries for a specific user, scoped to the requested date range.
/// Authorisation (self / supervisor / employer) is enforced in the handler using
/// <see cref="Domain.Entities.User.CanViewHoursOf"/>.
/// </summary>
public record GetTimeEntriesQuery(
    Guid TargetUserId,
    DateOnly? From,
    DateOnly? To
) : IRequest<Result<IReadOnlyList<TimeEntryDto>>>;

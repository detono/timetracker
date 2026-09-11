using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;

namespace TimeTracker.Application.TimeEntries.Commands.CreateTimeEntry;

/// <summary>
/// Logs a new block of worked time. If <see cref="TargetUserId"/> is null the entry is
/// logged for the calling user; an Employer may supply a different UserId to log on behalf
/// of someone else (e.g. correcting a missed entry).
/// </summary>
public record CreateTimeEntryCommand(
    Guid? TargetUserId,
    DateOnly WorkDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int BreakMinutes,
    string? Notes) : IRequest<Result<TimeEntryDto>>;

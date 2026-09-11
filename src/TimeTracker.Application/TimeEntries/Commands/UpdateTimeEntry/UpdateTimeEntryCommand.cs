using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;

namespace TimeTracker.Application.TimeEntries.Commands.UpdateTimeEntry;

public record UpdateTimeEntryCommand(
    Guid Id,
    Guid HourTypeId,
    DateOnly WorkDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int BreakMinutes,
    string? Notes) : IRequest<Result<TimeEntryDto>>;

using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Application.LeaveRequests.Commands.CreateLeaveRequest;

/// <summary>
/// Requests leave for the caller, from <see cref="StartDate"/> to <see cref="EndDate"/> inclusive.
/// For a single day, <see cref="FirstDayPart"/> says whether it's the whole day, the morning or the
/// afternoon. Over several days the first day can start in the afternoon and the last day end at noon.
/// </summary>
public record CreateLeaveRequestCommand(
    Guid HourTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Comment,
    DayPart FirstDayPart = DayPart.Full,
    DayPart LastDayPart = DayPart.Full) : IRequest<Result<LeaveRequestDto>>;

using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.LeaveRequests.Dtos;

namespace TimeTracker.Application.LeaveRequests.Commands.CreateLeaveRequest;

/// <summary>Requests leave for the caller, from <see cref="StartDate"/> to <see cref="EndDate"/> inclusive.</summary>
public record CreateLeaveRequestCommand(Guid HourTypeId, DateOnly StartDate, DateOnly EndDate, string? Comment)
    : IRequest<Result<LeaveRequestDto>>;

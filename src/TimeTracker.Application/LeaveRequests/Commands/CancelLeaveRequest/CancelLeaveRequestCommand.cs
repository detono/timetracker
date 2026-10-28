using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.LeaveRequests.Dtos;

namespace TimeTracker.Application.LeaveRequests.Commands.CancelLeaveRequest;

/// <summary>
/// Withdraws a pending request (the requester or a reviewer), or revokes an approved one
/// (reviewers only), which removes the time entries it booked.
/// </summary>
public record CancelLeaveRequestCommand(Guid Id, string? Comment) : IRequest<Result<LeaveRequestDto>>;

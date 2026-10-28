using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.LeaveRequests.Dtos;

namespace TimeTracker.Application.LeaveRequests.Commands.ReviewLeaveRequest;

public enum LeaveDecision {
    Approve,
    Reject
}

/// <summary>
/// Approves or rejects a pending request; only the requester's supervisor or an employer
/// may. Approving books the leave as time entries on each scheduled working day. Going
/// over the leave balance doesn't block approval: the request shows a warning instead.
/// </summary>
public record ReviewLeaveRequestCommand(Guid Id, LeaveDecision Decision, string? Comment)
    : IRequest<Result<LeaveRequestDto>>;

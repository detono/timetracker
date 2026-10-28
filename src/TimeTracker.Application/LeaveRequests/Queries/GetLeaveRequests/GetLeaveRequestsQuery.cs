using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Application.LeaveRequests.Queries.GetLeaveRequests;

public enum LeaveRequestScope {
    /// <summary>The caller's own requests.</summary>
    Mine,

    /// <summary>Requests the caller reviews: everyone's for employers, their team's for supervisors.</summary>
    ToReview
}

public record GetLeaveRequestsQuery(LeaveRequestScope Scope, LeaveRequestStatus? Status)
    : IRequest<Result<IReadOnlyList<LeaveRequestDto>>>;

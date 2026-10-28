using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.LeaveRequests.Queries.GetPendingLeaveCount;

/// <summary>How many requests are waiting for the caller's review; 0 for people who review nobody.</summary>
public record GetPendingLeaveCountQuery : IRequest<Result<int>>;

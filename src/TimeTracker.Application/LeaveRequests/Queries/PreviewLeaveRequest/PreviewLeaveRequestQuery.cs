using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.LeaveRequests.Dtos;

namespace TimeTracker.Application.LeaveRequests.Queries.PreviewLeaveRequest;

/// <summary>Shows the caller what a leave request would amount to, before they submit it.</summary>
public record PreviewLeaveRequestQuery(Guid HourTypeId, DateOnly StartDate, DateOnly EndDate)
    : IRequest<Result<LeavePreviewDto>>;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.LeaveRequests.Commands.CancelLeaveRequest;
using TimeTracker.Application.LeaveRequests.Commands.CreateLeaveRequest;
using TimeTracker.Application.LeaveRequests.Commands.ReviewLeaveRequest;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Application.LeaveRequests.Queries.GetLeaveRequests;
using TimeTracker.Application.LeaveRequests.Queries.GetPendingLeaveCount;
using TimeTracker.Application.LeaveRequests.Queries.PreviewLeaveRequest;
using TimeTracker.Domain.Enums;

namespace TimeTracker.API.Controllers;

/// <summary>
/// Leave requests. Anyone can request leave for themselves; the requester's supervisor or
/// any employer reviews it.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Route("api/leave-requests")]
public class LeaveRequestsController : ApiControllerBase
{
    /// <summary>The caller's own requests (scope=Mine) or the ones they review (scope=ToReview).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetAll(
        [FromQuery] LeaveRequestScope scope, [FromQuery] LeaveRequestStatus? status, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetLeaveRequestsQuery(scope, status), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>How many requests wait for the caller's review.</summary>
    [HttpGet("pending-count")]
    public async Task<ActionResult<int>> GetPendingCount(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPendingLeaveCountQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>The hours and balance effect of a request the caller is about to make.</summary>
    [HttpGet("preview")]
    public async Task<ActionResult<LeavePreviewDto>> Preview(
        [FromQuery] Guid hourTypeId, [FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate,
        [FromQuery] DayPart firstDayPart, [FromQuery] DayPart lastDayPart,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new PreviewLeaveRequestQuery(hourTypeId, startDate, endDate, firstDayPart, lastDayPart), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Create(CreateLeaveRequestCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Approves a pending request and books it as time entries.</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<LeaveRequestDto>> Approve(
        Guid id, [FromBody] LeaveCommentBody? body, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ReviewLeaveRequestCommand(id, LeaveDecision.Approve, body?.Comment), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<LeaveRequestDto>> Reject(
        Guid id, [FromBody] LeaveCommentBody? body, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ReviewLeaveRequestCommand(id, LeaveDecision.Reject, body?.Comment), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Withdraws a pending request, or revokes approved leave and removes its time entries.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<LeaveRequestDto>> Cancel(
        Guid id, [FromBody] LeaveCommentBody? body, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CancelLeaveRequestCommand(id, body?.Comment), cancellationToken);
        return HandleResult(result);
    }
}

public record LeaveCommentBody(string? Comment);

using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Application.LockedMonths;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LeaveRequests.Commands.CancelLeaveRequest;

public class CancelLeaveRequestCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    LeaveService leave,
    MonthLocks monthLocks,
    LeaveRequestMapper mapper,
    LeaveNotifications notifications
) : IRequestHandler<CancelLeaveRequestCommand, Result<LeaveRequestDto>> {
    public async Task<Result<LeaveRequestDto>> Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken) {
        var leaveRequest = await unitOfWork.LeaveRequests.GetByIdAsync(request.Id, cancellationToken);
        var requester = leaveRequest is null
            ? null
            : await unitOfWork.Users.GetByIdAsync(leaveRequest.UserId, cancellationToken);
        if (leaveRequest is null || requester is null) {
            return Result<LeaveRequestDto>.Failure("Leave request was not found.", ResultErrorType.NotFound);
        }

        var canReview = leave.CanReview(requester);
        if (!canReview && requester.Id != currentUser.UserId) {
            return Result<LeaveRequestDto>.Failure("Leave request was not found.", ResultErrorType.NotFound);
        }

        if (leaveRequest.Status is not (LeaveRequestStatus.Pending or LeaveRequestStatus.Approved)) {
            return Result<LeaveRequestDto>.Failure(
                $"This request was already {leaveRequest.Status.ToString().ToLowerInvariant()}.", ResultErrorType.Conflict);
        }

        if (!mapper.CanCancel(leaveRequest, canReview)) {
            return Result<LeaveRequestDto>.Failure(
                "Approved leave can only be cancelled by your supervisor or an employer.", ResultErrorType.Forbidden);
        }

        if (leaveRequest.Status == LeaveRequestStatus.Approved) {
            // Revoking removes booked hours; withdrawing a pending request doesn't touch any.
            if (await monthLocks.FindLockedAsync(leaveRequest.StartDate, leaveRequest.EndDate, cancellationToken) is { } locked) {
                return Result<LeaveRequestDto>.Failure(locked, ResultErrorType.Conflict);
            }

            foreach (var entry in await unitOfWork.TimeEntries.GetByLeaveRequestIdAsync(leaveRequest.Id, cancellationToken)) {
                unitOfWork.TimeEntries.Remove(entry);
            }
        }

        leaveRequest.Cancel(currentUser.UserId, request.Comment, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var canceller = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        var hourType = await unitOfWork.HourTypes.GetByIdAsync(leaveRequest.HourTypeId, cancellationToken);
        if (canceller is not null && hourType is not null) {
            await notifications.RequestReviewedAsync(
                leaveRequest, requester, canceller, hourType, cancellationToken);
        }

        return Result<LeaveRequestDto>.Success(await mapper.ToDtoAsync(leaveRequest, cancellationToken));
    }
}

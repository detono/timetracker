using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Application.LockedMonths;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LeaveRequests.Commands.ReviewLeaveRequest;

public class ReviewLeaveRequestCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    LeaveService leave,
    MonthLocks monthLocks,
    LeaveRequestMapper mapper,
    LeaveNotifications notifications
) : IRequestHandler<ReviewLeaveRequestCommand, Result<LeaveRequestDto>> {
    public async Task<Result<LeaveRequestDto>> Handle(ReviewLeaveRequestCommand request, CancellationToken cancellationToken) {
        var leaveRequest = await unitOfWork.LeaveRequests.GetByIdAsync(request.Id, cancellationToken);
        var requester = leaveRequest is null
            ? null
            : await unitOfWork.Users.GetByIdAsync(leaveRequest.UserId, cancellationToken);
        if (leaveRequest is null || requester is null) {
            return Result<LeaveRequestDto>.Failure("Leave request was not found.", ResultErrorType.NotFound);
        }

        if (!leave.CanReview(requester)) {
            return Result<LeaveRequestDto>.Failure(
                "Only their supervisor or an employer can review this request.", ResultErrorType.Forbidden);
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending) {
            return Result<LeaveRequestDto>.Failure(
                $"This request was already {leaveRequest.Status.ToString().ToLowerInvariant()}.", ResultErrorType.Conflict);
        }

        var reviewer = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        var hourType = await unitOfWork.HourTypes.GetByIdAsync(leaveRequest.HourTypeId, cancellationToken);
        if (reviewer is null || hourType is null) {
            return Result<LeaveRequestDto>.Failure("Reviewer or hour type was not found.", ResultErrorType.NotFound);
        }

        if (request.Decision == LeaveDecision.Approve) {
            // Approving books hours; rejecting doesn't touch any, so it's always allowed.
            if (await monthLocks.FindLockedAsync(leaveRequest.StartDate, leaveRequest.EndDate, cancellationToken) is { } locked) {
                return Result<LeaveRequestDto>.Failure(locked, ResultErrorType.Conflict);
            }

            // Book what the schedule says now: it may have changed since the request was made.
            var schedule = await leave.GetScheduleAsync(requester.Id, cancellationToken);
            var holidays = await leave.GetHolidaysAsync(leaveRequest.StartDate, leaveRequest.EndDate, cancellationToken);
            var days = LeavePlan.Plan(schedule, leaveRequest.StartDate, leaveRequest.EndDate,
                leaveRequest.FirstDayPart, leaveRequest.LastDayPart, holidays);
            if (days.Count == 0) {
                return Result<LeaveRequestDto>.Failure(
                    "This period no longer has any working days in the work schedule. Reject the request instead.");
            }

            foreach (var day in days) {
                var entry = TimeEntry.Create(
                    requester.Id, hourType.Id, day.Date, day.StartTime, day.EndTime, day.BreakMinutes,
                    notes: leaveRequest.Comment);
                entry.LinkToLeaveRequest(leaveRequest.Id);
                await unitOfWork.TimeEntries.AddAsync(entry, cancellationToken);
            }

            leaveRequest.Approve(reviewer.Id, request.Comment, LeaveService.HoursOf(days), clock.UtcNow);
        } else {
            leaveRequest.Reject(reviewer.Id, request.Comment, clock.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notifications.RequestReviewedAsync(
            leaveRequest, requester, reviewer, hourType, cancellationToken);

        return Result<LeaveRequestDto>.Success(await mapper.ToDtoAsync(leaveRequest, cancellationToken));
    }
}

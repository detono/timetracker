using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LeaveRequests.Commands.CreateLeaveRequest;

public class CreateLeaveRequestCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    LeaveService leave,
    LeaveRequestMapper mapper,
    LeaveNotifications notifications
) : IRequestHandler<CreateLeaveRequestCommand, Result<LeaveRequestDto>> {
    public async Task<Result<LeaveRequestDto>> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken) {
        // Employers log their own time off directly as hours; there's nobody to approve it.
        if (currentUser.Role == UserRole.Employer) {
            return Result<LeaveRequestDto>.Failure(
                "Employers don't request leave; log it directly as hours instead.", ResultErrorType.Forbidden);
        }

        var requester = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (requester is null) {
            return Result<LeaveRequestDto>.Failure("Your account was not found.", ResultErrorType.NotFound);
        }

        var hourType = await unitOfWork.HourTypes.GetByIdAsync(request.HourTypeId, cancellationToken);
        if (hourType is null || !hourType.IsActive || !hourType.IsLeave) {
            return Result<LeaveRequestDto>.Failure("This hour type can't be requested as leave.");
        }

        var overlapping = await unitOfWork.LeaveRequests.GetActiveOverlappingAsync(
            requester.Id, request.StartDate, request.EndDate, cancellationToken);
        if (overlapping.Count > 0) {
            return Result<LeaveRequestDto>.Failure(
                "You already have a pending or approved leave request in this period.", ResultErrorType.Conflict);
        }

        var schedule = await leave.GetScheduleAsync(requester.Id, cancellationToken);
        var holidays = await leave.GetHolidaysAsync(request.StartDate, request.EndDate, cancellationToken);
        var hours = LeaveService.HoursOf(LeavePlan.Plan(
            schedule, request.StartDate, request.EndDate, request.FirstDayPart, request.LastDayPart, holidays));
        if (hours == 0) {
            return Result<LeaveRequestDto>.Failure("You have no working days in this period.");
        }

        var leaveRequest = LeaveRequest.Create(
            requester.Id, hourType.Id, request.StartDate, request.EndDate, hours, request.Comment,
            request.FirstDayPart, request.LastDayPart);
        await unitOfWork.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifications.RequestSubmittedAsync(leaveRequest, requester, LeaveRequestMapper.NameOf(hourType), cancellationToken);

        return Result<LeaveRequestDto>.Success(await mapper.ToDtoAsync(leaveRequest, cancellationToken));
    }
}

using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LeaveRequests.Queries.PreviewLeaveRequest;

public class PreviewLeaveRequestQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    LeaveService leave
) : IRequestHandler<PreviewLeaveRequestQuery, Result<LeavePreviewDto>> {
    public async Task<Result<LeavePreviewDto>> Handle(PreviewLeaveRequestQuery request, CancellationToken cancellationToken) {
        if (currentUser.Role == UserRole.Employer) {
            return Result<LeavePreviewDto>.Failure(
                "Employers don't request leave; log it directly as hours instead.", ResultErrorType.Forbidden);
        }

        if (request.EndDate < request.StartDate ||
            request.EndDate.DayNumber - request.StartDate.DayNumber >= LeaveRequest.MaxRangeDays) {
            return Result<LeavePreviewDto>.Failure("Choose an end date on or after the start date, at most a year later.");
        }

        var hourType = await unitOfWork.HourTypes.GetByIdAsync(request.HourTypeId, cancellationToken);
        if (hourType is null || !hourType.IsActive || !hourType.IsLeave) {
            return Result<LeavePreviewDto>.Failure("This hour type can't be requested as leave.");
        }

        var schedule = await leave.GetScheduleAsync(currentUser.UserId, cancellationToken);
        var holidays = await leave.GetHolidaysAsync(request.StartDate, request.EndDate, cancellationToken);
        var firstDayPart = request.FirstDayPart;
        var lastDayPart = request.StartDate == request.EndDate ? firstDayPart : request.LastDayPart;
        if (request.StartDate != request.EndDate &&
            (firstDayPart == DayPart.Morning || lastDayPart == DayPart.Afternoon)) {
            return Result<LeavePreviewDto>.Failure(
                "Over several days, the first day can only start in the afternoon and the last day only end at noon.");
        }

        var days = LeavePlan.Plan(schedule, request.StartDate, request.EndDate, firstDayPart, lastDayPart, holidays);
        var hours = LeaveService.HoursOf(days);
        var workingDays = days.Sum(d => d.Fraction);
        var overlaps = (await unitOfWork.LeaveRequests.GetActiveOverlappingAsync(
            currentUser.UserId, request.StartDate, request.EndDate, cancellationToken)).Count > 0;

        if (hours == 0) {
            return Result<LeavePreviewDto>.Success(
                new LeavePreviewDto(0, 0, hourType.DeductsFromLeaveBalance, null, false, overlaps));
        }

        // An unsaved request, only used to check the balance.
        var draft = LeaveRequest.Create(currentUser.UserId, hourType.Id, request.StartDate, request.EndDate, hours, null,
            firstDayPart, lastDayPart);
        var (balance, exceeds) = await leave.CheckBalanceAsync(draft, hourType, schedule, cancellationToken);

        return Result<LeavePreviewDto>.Success(new LeavePreviewDto(
            workingDays, hours, hourType.DeductsFromLeaveBalance, balance?.RemainingHours, exceeds, overlaps));
    }
}

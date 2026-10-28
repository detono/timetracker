using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Application.WorkSchedules.Dtos;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.WorkSchedules.Queries.GetWorkSchedule;

public class GetWorkScheduleQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    LeaveService leave
) : IRequestHandler<GetWorkScheduleQuery, Result<WorkScheduleDto>> {
    public async Task<Result<WorkScheduleDto>> Handle(GetWorkScheduleQuery request, CancellationToken cancellationToken) {
        var caller = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        var target = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (caller is null || target is null) {
            return Result<WorkScheduleDto>.Failure("User was not found.", ResultErrorType.NotFound);
        }

        if (!caller.CanViewHoursOf(target.Id, target.SupervisorId)) {
            return Result<WorkScheduleDto>.Failure(
                "You are not allowed to view this work schedule.", ResultErrorType.Forbidden);
        }

        var (schedule, source) = await leave.ResolveScheduleAsync(target.Id, cancellationToken);
        return Result<WorkScheduleDto>.Success(schedule.ToDto(target.Id, source));
    }
}

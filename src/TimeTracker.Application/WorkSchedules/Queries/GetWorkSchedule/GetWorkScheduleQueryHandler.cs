using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.WorkSchedules.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.WorkSchedules.Queries.GetWorkSchedule;

public class GetWorkScheduleQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
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

        var saved = await unitOfWork.WorkSchedules.GetForUserAsync(target.Id, cancellationToken);
        var schedule = saved ?? WorkSchedule.Default(target.Id);
        return Result<WorkScheduleDto>.Success(schedule.ToDto(isDefault: saved is null));
    }
}

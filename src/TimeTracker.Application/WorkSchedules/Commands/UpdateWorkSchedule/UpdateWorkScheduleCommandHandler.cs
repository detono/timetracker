using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.WorkSchedules.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.WorkSchedules.Commands.UpdateWorkSchedule;

public class UpdateWorkScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateWorkScheduleCommand, Result<WorkScheduleDto>> {
    public async Task<Result<WorkScheduleDto>> Handle(UpdateWorkScheduleCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<WorkScheduleDto>.Failure("Only an employer can change work schedules.", ResultErrorType.Forbidden);
        }

        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) {
            return Result<WorkScheduleDto>.Failure("User was not found.", ResultErrorType.NotFound);
        }

        var days = request.Days
            .Select(d => new WorkScheduleDay(d.DayOfWeek, d.StartTime, d.EndTime, d.BreakMinutes))
            .ToList();

        var schedule = await unitOfWork.WorkSchedules.GetForUserAsync(user.Id, cancellationToken);
        if (schedule is null) {
            schedule = WorkSchedule.Create(user.Id, days);
            await unitOfWork.WorkSchedules.AddAsync(schedule, cancellationToken);
        } else {
            schedule.SetDays(days);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<WorkScheduleDto>.Success(schedule.ToDto(isDefault: false));
    }
}

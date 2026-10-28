using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
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

        if (request.UserId is { } userId && await unitOfWork.Users.GetByIdAsync(userId, cancellationToken) is null) {
            return Result<WorkScheduleDto>.Failure("User was not found.", ResultErrorType.NotFound);
        }

        var days = request.Days
            .Select(d => new WorkScheduleDay(d.DayOfWeek, d.StartTime, d.EndTime, d.BreakMinutes))
            .ToList();

        var schedule = request.UserId is { } id
            ? await unitOfWork.WorkSchedules.GetForUserAsync(id, cancellationToken)
            : await unitOfWork.WorkSchedules.GetCompanyDefaultAsync(cancellationToken);

        if (schedule is null) {
            schedule = request.UserId is { } newUserId
                ? WorkSchedule.Create(newUserId, days)
                : WorkSchedule.CreateCompanyDefault(days);
            await unitOfWork.WorkSchedules.AddAsync(schedule, cancellationToken);
        } else {
            schedule.SetDays(days);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var source = request.UserId is null ? WorkScheduleSource.Company : WorkScheduleSource.Personal;
        return Result<WorkScheduleDto>.Success(schedule.ToDto(request.UserId, source));
    }
}

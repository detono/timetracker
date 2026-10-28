using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Application.WorkSchedules.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.WorkSchedules.Commands.ResetWorkSchedule;

public class ResetWorkScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    LeaveService leave
) : IRequestHandler<ResetWorkScheduleCommand, Result<WorkScheduleDto>> {
    public async Task<Result<WorkScheduleDto>> Handle(ResetWorkScheduleCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<WorkScheduleDto>.Failure("Only an employer can change work schedules.", ResultErrorType.Forbidden);
        }

        if (await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken) is null) {
            return Result<WorkScheduleDto>.Failure("User was not found.", ResultErrorType.NotFound);
        }

        if (await unitOfWork.WorkSchedules.GetForUserAsync(request.UserId, cancellationToken) is { } own) {
            unitOfWork.WorkSchedules.Remove(own);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var (schedule, source) = await leave.ResolveCompanyScheduleAsync(cancellationToken);
        return Result<WorkScheduleDto>.Success(schedule.ToDto(request.UserId, source));
    }
}

using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Application.WorkSchedules.Dtos;

namespace TimeTracker.Application.WorkSchedules.Queries.GetCompanyWorkSchedule;

public class GetCompanyWorkScheduleQueryHandler(LeaveService leave)
    : IRequestHandler<GetCompanyWorkScheduleQuery, Result<WorkScheduleDto>> {
    public async Task<Result<WorkScheduleDto>> Handle(GetCompanyWorkScheduleQuery request, CancellationToken cancellationToken) {
        var (schedule, source) = await leave.ResolveCompanyScheduleAsync(cancellationToken);
        return Result<WorkScheduleDto>.Success(schedule.ToDto(null, source));
    }
}

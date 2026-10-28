using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.WorkSchedules.Dtos;

namespace TimeTracker.Application.WorkSchedules.Commands.UpdateWorkSchedule;

/// <summary>
/// Employer-only: sets someone's own weekly working hours, or the company's when
/// <see cref="UserId"/> is null. Weekdays left out are days off.
/// </summary>
public record UpdateWorkScheduleCommand(Guid? UserId, IReadOnlyList<WorkScheduleDayDto> Days)
    : IRequest<Result<WorkScheduleDto>>;

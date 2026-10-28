using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.WorkSchedules.Dtos;

namespace TimeTracker.Application.WorkSchedules.Queries.GetWorkSchedule;

/// <summary>
/// Someone's weekly working hours: their own, or the company's if they have none.
/// Visible to themselves, their supervisor and employers.
/// </summary>
public record GetWorkScheduleQuery(Guid UserId) : IRequest<Result<WorkScheduleDto>>;

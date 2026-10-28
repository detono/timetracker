using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.WorkSchedules.Dtos;

namespace TimeTracker.Application.WorkSchedules.Queries.GetWorkSchedule;

/// <summary>Someone's weekly working hours. Visible to themselves, their supervisor and employers.</summary>
public record GetWorkScheduleQuery(Guid UserId) : IRequest<Result<WorkScheduleDto>>;

using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.WorkSchedules.Dtos;

namespace TimeTracker.Application.WorkSchedules.Queries.GetCompanyWorkSchedule;

/// <summary>The company's default working hours, which everyone without their own schedule follows.</summary>
public record GetCompanyWorkScheduleQuery : IRequest<Result<WorkScheduleDto>>;

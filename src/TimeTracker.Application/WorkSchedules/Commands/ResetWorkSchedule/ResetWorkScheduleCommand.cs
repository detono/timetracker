using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.WorkSchedules.Dtos;

namespace TimeTracker.Application.WorkSchedules.Commands.ResetWorkSchedule;

/// <summary>Employer-only: removes someone's own schedule so they follow the company's again.</summary>
public record ResetWorkScheduleCommand(Guid UserId) : IRequest<Result<WorkScheduleDto>>;

using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.LeaveBalances.Commands.SetLeaveAllowance;

/// <summary>Employer-only: sets how many leave hours someone gets in a year.</summary>
public record SetLeaveAllowanceCommand(Guid UserId, int Year, decimal AllowanceHours) : IRequest<Result>;

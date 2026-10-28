using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.LeaveBalances.Dtos;

namespace TimeTracker.Application.LeaveBalances.Queries.GetLeaveBalances;

/// <summary>
/// Leave balances for a year: every employee's for employers (employers themselves don't
/// request leave), otherwise the caller's own plus those of the people they supervise.
/// </summary>
public record GetLeaveBalancesQuery(int Year) : IRequest<Result<IReadOnlyList<LeaveBalanceDto>>>;

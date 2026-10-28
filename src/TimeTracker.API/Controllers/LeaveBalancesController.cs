using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.LeaveBalances.Commands.SetLeaveAllowance;
using TimeTracker.Application.LeaveBalances.Dtos;
using TimeTracker.Application.LeaveBalances.Queries.GetLeaveBalances;

namespace TimeTracker.API.Controllers;

[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Route("api/leave-balances")]
public class LeaveBalancesController : ApiControllerBase
{
    /// <summary>Balances for a year: everyone's for employers, else the caller's and their team's.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceDto>>> GetAll(
        [FromQuery] int year, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetLeaveBalancesQuery(year), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Sets someone's leave allowance for a year, in hours. Employer only.</summary>
    [HttpPut("{userId:guid}/{year:int}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult> SetAllowance(
        Guid userId, int year, [FromBody] SetAllowanceBody body, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetLeaveAllowanceCommand(userId, year, body.AllowanceHours), cancellationToken);
        return HandleResult(result);
    }
}

public record SetAllowanceBody(decimal AllowanceHours);

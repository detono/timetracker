using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.LockedMonths;

namespace TimeTracker.API.Controllers;

/// <summary>
/// Months whose hours are final. Nobody can change hours in a locked month, employers
/// included, until an employer unlocks it.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Route("api/locked-months")]
public class LockedMonthsController : ApiControllerBase
{
    /// <summary>Every locked month of the company.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LockedMonthDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetLockedMonthsQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Locks a month that has started. Locking an already locked month changes nothing.</summary>
    [HttpPut("{year:int}/{month:int}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<LockedMonthDto>> Lock(int year, int month, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new LockMonthCommand(year, month), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Unlocks a month so its hours can be changed again.</summary>
    [HttpDelete("{year:int}/{month:int}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult> Unlock(int year, int month, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UnlockMonthCommand(year, month), cancellationToken);
        return HandleResult(result);
    }
}

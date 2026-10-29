using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.AuditLog;

namespace TimeTracker.API.Controllers;

/// <summary>
/// The company's activity log: locked and unlocked months, hours changed on someone else's
/// behalf, password resets and (de)activated accounts. Employer only; entries never change.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Authorize(Roles = "Employer")]
[Route("api/audit-log")]
public class AuditLogController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditEntryDto>>> Get(
        [FromQuery] DateTime? before, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetAuditLogQuery(before, take), cancellationToken);
        return HandleResult(result);
    }
}

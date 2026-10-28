using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.HoursOverview.Dtos;
using TimeTracker.Application.HoursOverview.Queries.GetMyHoursOverview;
using TimeTracker.Application.HoursOverview.Queries.GetTeamHoursOverview;

namespace TimeTracker.API.Controllers;

/// <summary>
/// Worked versus scheduled hours, calculated on the fly. Pass <c>asOf</c> (the caller's
/// local date) so "today" matches their time zone; it defaults to today in UTC.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Route("api/hours-overview")]
public class HoursOverviewController : ApiControllerBase
{
    /// <summary>The caller's week, month and year so far, plus the current week day by day.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<MyHoursOverviewDto>> GetMine([FromQuery] DateOnly? asOf, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMyHoursOverviewQuery(asOf), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>One row per person the caller manages: everyone for employers, their team for supervisors.</summary>
    [HttpGet("team")]
    public async Task<ActionResult<IReadOnlyList<TeamMemberHoursDto>>> GetTeam(
        [FromQuery] DateOnly? asOf, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTeamHoursOverviewQuery(asOf), cancellationToken);
        return HandleResult(result);
    }
}

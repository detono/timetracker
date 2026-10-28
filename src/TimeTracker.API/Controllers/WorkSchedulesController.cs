using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.WorkSchedules.Commands.UpdateWorkSchedule;
using TimeTracker.Application.WorkSchedules.Dtos;
using TimeTracker.Application.WorkSchedules.Queries.GetWorkSchedule;

namespace TimeTracker.API.Controllers;

/// <summary>Weekly working hours per person, used to turn leave requests into hours.</summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Route("api/work-schedules")]
public class WorkSchedulesController : ApiControllerBase
{
    /// <summary>Someone's schedule (the default one if none was saved). Self, supervisor or employer.</summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<WorkScheduleDto>> Get(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWorkScheduleQuery(userId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Replaces someone's schedule. Weekdays left out are days off. Employer only.</summary>
    [HttpPut("{userId:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<WorkScheduleDto>> Update(
        Guid userId, [FromBody] IReadOnlyList<WorkScheduleDayDto> days, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateWorkScheduleCommand(userId, days), cancellationToken);
        return HandleResult(result);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.WorkSchedules.Commands.ResetWorkSchedule;
using TimeTracker.Application.WorkSchedules.Commands.UpdateWorkSchedule;
using TimeTracker.Application.WorkSchedules.Dtos;
using TimeTracker.Application.WorkSchedules.Queries.GetCompanyWorkSchedule;
using TimeTracker.Application.WorkSchedules.Queries.GetWorkSchedule;

namespace TimeTracker.API.Controllers;

/// <summary>Weekly working hours per person, used to turn leave requests into hours.</summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Route("api/work-schedules")]
public class WorkSchedulesController : ApiControllerBase
{
    /// <summary>The company's default schedule, which everyone without their own follows.</summary>
    [HttpGet("company")]
    public async Task<ActionResult<WorkScheduleDto>> GetCompany(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCompanyWorkScheduleQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Replaces the company's default schedule. Employer only.</summary>
    [HttpPut("company")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<WorkScheduleDto>> UpdateCompany(
        [FromBody] IReadOnlyList<WorkScheduleDayDto> days, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateWorkScheduleCommand(null, days), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Someone's schedule: their own, or the company's. Self, supervisor or employer.</summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<WorkScheduleDto>> Get(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWorkScheduleQuery(userId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Gives someone their own schedule. Weekdays left out are days off. Employer only.</summary>
    [HttpPut("{userId:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<WorkScheduleDto>> Update(
        Guid userId, [FromBody] IReadOnlyList<WorkScheduleDayDto> days, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateWorkScheduleCommand(userId, days), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Removes someone's own schedule so they follow the company's again. Employer only.</summary>
    [HttpDelete("{userId:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<WorkScheduleDto>> Reset(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ResetWorkScheduleCommand(userId), cancellationToken);
        return HandleResult(result);
    }
}

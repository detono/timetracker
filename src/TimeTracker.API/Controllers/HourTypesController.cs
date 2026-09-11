using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.HourTypes.Commands.ActivateHourType;
using TimeTracker.Application.HourTypes.Commands.CreateHourType;
using TimeTracker.Application.HourTypes.Commands.DeactivateHourType;
using TimeTracker.Application.HourTypes.Commands.UpdateHourType;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.HourTypes.Queries.GetHourTypes;

namespace TimeTracker.API.Controllers;

[Authorize]
public class HourTypesController : ApiControllerBase
{
    /// <summary>
    /// Lists hour types. Any authenticated user can call this (they need it to log hours).
    /// Pass includeInactive=true to also see retired types - only honoured for Employers.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HourTypeDto>>> GetAll(
        [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetHourTypesQuery(includeInactive), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Creates a new hour type. Employer only.</summary>
    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<HourTypeDto>> Create(CreateHourTypeCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Renames/recolors an existing hour type. Employer only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<HourTypeDto>> Update(Guid id, UpdateHourTypeCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "Route id and body id must match." });
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Retires an hour type so it no longer appears as an option for new entries. Employer only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeactivateHourTypeCommand(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Brings a retired hour type back. Employer only.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ActivateHourTypeCommand(id), cancellationToken);
        return HandleResult(result);
    }
}

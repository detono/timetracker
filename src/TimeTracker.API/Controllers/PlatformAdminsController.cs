using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.PlatformAdmins.Commands.CreatePlatformAdmin;
using TimeTracker.Application.PlatformAdmins.Commands.SetPlatformAdminActive;
using TimeTracker.Application.PlatformAdmins.Queries.GetPlatformAdmins;
using TimeTracker.Application.Users.Dtos;

namespace TimeTracker.API.Controllers;

/// <summary>Platform admin account management. Every endpoint here is PlatformAdmin-only.</summary>
[Authorize(Roles = "PlatformAdmin")]
[Route("api/platform-admins")]
public class PlatformAdminsController : ApiControllerBase
{
    /// <summary>Lists every platform admin, active and deactivated.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPlatformAdminsQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Creates another platform admin.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreatePlatformAdminCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Deactivates another platform admin. You cannot deactivate yourself.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetPlatformAdminActiveCommand(id, false), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Reinstates a previously deactivated platform admin.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetPlatformAdminActiveCommand(id, true), cancellationToken);
        return HandleResult(result);
    }
}

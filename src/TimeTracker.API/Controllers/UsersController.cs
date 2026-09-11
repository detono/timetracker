using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.Users.Commands.ActivateUser;
using TimeTracker.Application.Users.Commands.AssignSupervisor;
using TimeTracker.Application.Users.Commands.DeactivateUser;
using TimeTracker.Application.Users.Commands.RegisterUser;
using TimeTracker.Application.Users.Commands.ResetPassword;
using TimeTracker.Application.Users.Dtos;
using TimeTracker.Application.Users.Queries.GetUsers;

namespace TimeTracker.API.Controllers;

/// <summary>Account administration. Every endpoint here is Employer-only.</summary>
[Authorize(Roles = "Employer")]
public class UsersController : ApiControllerBase
{
    /// <summary>Lists every user account, active and deactivated.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUsersQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Creates a new Employee or Employer account.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deactivates ("soft-deletes") an account. Deactivated users can no longer log in;
    /// their historical time entries are preserved for reporting.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeactivateUserCommand(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Reinstates a previously deactivated account.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ActivateUserCommand(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Grants an employee supervisory authority over another employee's hours.</summary>
    [HttpPost("{employeeId:guid}/supervisor")]
    public async Task<ActionResult> AssignSupervisor(Guid employeeId, [FromBody] Guid? supervisorId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AssignSupervisorCommand(employeeId, supervisorId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Sets a new password for another user, without needing their current one.</summary>
    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequestBody body, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ResetPasswordCommand(id, body.NewPassword), cancellationToken);
        return HandleResult(result);
    }
}

public record ResetPasswordRequestBody(string NewPassword);

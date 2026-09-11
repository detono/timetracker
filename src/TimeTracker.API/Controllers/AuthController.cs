using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.Auth.Commands.ChangePassword;
using TimeTracker.Application.Auth.Commands.Login;
using TimeTracker.Application.Auth.Dtos;

namespace TimeTracker.API.Controllers;

public class AuthController : ApiControllerBase {
    /// <summary>Authenticates a user and returns a JWT bearer token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResultDto>> Login(LoginCommand command, CancellationToken cancellationToken) {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Changes a user's password.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken) {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TimeTracker.API.Authorization;
using TimeTracker.Application.Auth.Commands.ChangePassword;
using TimeTracker.Application.Auth.Commands.Login;
using TimeTracker.Application.Auth.Commands.RequestPasswordReset;
using TimeTracker.Application.Auth.Commands.ResetForgottenPassword;
using TimeTracker.Application.Auth.Commands.SetLanguage;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Auth.Queries.GetPasswordResetAvailability;

namespace TimeTracker.API.Controllers;

public class AuthController : ApiControllerBase
{
    /// <summary>Authenticates a user and returns a JWT bearer token.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResultDto>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Saves the signed-in user's language, used for the app and for the emails they get.</summary>
    [HttpPut("language")]
    [Authorize]
    public async Task<ActionResult> SetLanguage(SetLanguageCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lets the currently signed-in user change their own password. Their other sessions are
    /// signed out; the response is a new sign-in for this one.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResultDto>> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Whether "forgot password" can work here: some company has email set up.</summary>
    [HttpGet("password-reset")]
    [AllowAnonymous]
    public async Task<ActionResult<PasswordResetAvailabilityDto>> GetPasswordResetAvailability(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPasswordResetAvailabilityQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Emails a reset code if the account exists and its company has email set up. Always
    /// answers 204, so it doesn't reveal which addresses have an account.
    /// </summary>
    [HttpPost("password-reset")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [AllowAnonymous]
    public async Task<ActionResult> RequestPasswordReset(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Sets a new password with an emailed code. Signs out every existing session.</summary>
    [HttpPost("password-reset/confirm")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [AllowAnonymous]
    public async Task<ActionResult> ResetForgottenPassword(ResetForgottenPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

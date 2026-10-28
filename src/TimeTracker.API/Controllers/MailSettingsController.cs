using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.MailSettings.Commands.SendTestEmail;
using TimeTracker.Application.MailSettings.Commands.UpdateMailSettings;
using TimeTracker.Application.MailSettings.Dtos;
using TimeTracker.Application.MailSettings.Queries.GetMailSettings;

namespace TimeTracker.API.Controllers;

/// <summary>The company's outgoing email settings. Employer only; secrets are never returned.</summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Authorize(Roles = "Employer")]
[Route("api/mail-settings")]
public class MailSettingsController : ApiControllerBase
{
    /// <summary>Returns the settings, with only whether each secret is set.</summary>
    [HttpGet]
    public async Task<ActionResult<MailSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMailSettingsQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Saves the settings. Leave a secret empty to keep the stored one.</summary>
    [HttpPut]
    public async Task<ActionResult<MailSettingsDto>> Update(UpdateMailSettingsCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Sends a test email to the caller with the saved settings; a 400 explains why it failed.</summary>
    [HttpPost("test")]
    public async Task<ActionResult> SendTest(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SendTestEmailCommand(), cancellationToken);
        return HandleResult(result);
    }
}

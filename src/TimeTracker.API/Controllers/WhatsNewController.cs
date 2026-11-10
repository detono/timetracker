using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.WhatsNew;

namespace TimeTracker.API.Controllers;

/// <summary>
/// The "What's new" notes shown once after an update, from CHANGELOG.md. Any signed-in user;
/// what they've seen is remembered on their account.
/// </summary>
[Authorize]
[Route("api/whats-new")]
public class WhatsNewController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WhatsNewDto>> Get(CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(new GetWhatsNewQuery(), cancellationToken));

    [HttpPost("seen")]
    public async Task<ActionResult> MarkSeen(MarkWhatsNewSeenCommand command, CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(command, cancellationToken));
}

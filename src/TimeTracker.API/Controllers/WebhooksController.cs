using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.Webhooks;

namespace TimeTracker.API.Controllers;

/// <summary>
/// Addresses this app calls when something happens, like leave being approved. Employer only;
/// secrets are only returned by the secret endpoints.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Authorize(Roles = "Employer")]
[Route("api/webhooks")]
public class WebhooksController : ApiControllerBase
{
    /// <summary>The webhooks, the events they can listen to, and the latest calls.</summary>
    [HttpGet]
    public async Task<ActionResult<WebhooksOverviewDto>> Get(CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(new GetWebhooksQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<WebhookDto>> Create(SaveWebhookBody body, CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(body.ToCommand(null), cancellationToken));

    /// <summary>Leave the header value empty to keep the stored one.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WebhookDto>> Update(Guid id, SaveWebhookBody body, CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(body.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(new DeleteWebhookCommand(id), cancellationToken));

    /// <summary>The signing secret, for setting up the receiving side.</summary>
    [HttpGet("{id:guid}/secret")]
    public async Task<ActionResult<WebhookSecretDto>> GetSecret(Guid id, CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(new GetWebhookSecretQuery(id), cancellationToken));

    /// <summary>Replaces the signing secret; the old one stops working right away.</summary>
    [HttpPost("{id:guid}/secret")]
    public async Task<ActionResult<WebhookSecretDto>> RotateSecret(Guid id, CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(new RotateWebhookSecretCommand(id), cancellationToken));

    /// <summary>Sends a made-up event right away and returns how it went.</summary>
    [HttpPost("{id:guid}/test")]
    public async Task<ActionResult<WebhookDeliveryDto>> Test(Guid id, CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(new TestWebhookCommand(id), cancellationToken));

    /// <summary>Queues a failed call again, with a fresh set of retries.</summary>
    [HttpPost("deliveries/{id:guid}/retry")]
    public async Task<ActionResult> Retry(Guid id, CancellationToken cancellationToken) =>
        HandleResult(await Mediator.Send(new RetryWebhookDeliveryCommand(id), cancellationToken));
}

public record SaveWebhookBody(
    string Name, string Url, IReadOnlyList<string> Events, bool IsEnabled, string? HeaderName, string? HeaderValue)
{
    public SaveWebhookCommand ToCommand(Guid? id) => new(id, Name, Url, Events ?? Array.Empty<string>(), IsEnabled, HeaderName, HeaderValue);
}

using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Common.Interfaces;

/// <summary>Makes one signed call to a webhook's address.</summary>
public interface IWebhookSender {
    Task<WebhookCallResult> SendAsync(Webhook webhook, WebhookDelivery delivery, CancellationToken cancellationToken);
}

/// <param name="Retry">Whether trying again later could help (not for e.g. a 400, or an address that isn't allowed).</param>
public record WebhookCallResult(bool Succeeded, int? StatusCode, string? Error, bool Retry) {
    public static WebhookCallResult Success(int statusCode) => new(true, statusCode, null, false);
}

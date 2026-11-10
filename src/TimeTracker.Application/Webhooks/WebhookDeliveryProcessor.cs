using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Webhooks;

/// <summary>Tries one queued delivery and records how it went. Runs inside the delivery's company.</summary>
public class WebhookDeliveryProcessor(IUnitOfWork unitOfWork, IWebhookSender sender, IDateTimeProvider clock) {
    /// <summary>How long a delivery being sent is hidden from other workers.</summary>
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(2);

    public async Task ProcessAsync(Guid deliveryId, CancellationToken cancellationToken) {
        var delivery = await unitOfWork.Webhooks.GetDeliveryAsync(deliveryId, cancellationToken);
        if (delivery is not { Status: WebhookDeliveryStatus.Pending }) {
            return;
        }

        var webhook = await unitOfWork.Webhooks.GetByIdAsync(delivery.WebhookId, cancellationToken);
        var tenant = await unitOfWork.Tenants.GetByIdAsync(delivery.TenantId, cancellationToken);
        if (webhook is not { IsEnabled: true } || tenant is not { IsActive: true }) {
            delivery.RecordFailure(clock.UtcNow, null,
                tenant is not { IsActive: true } ? "The company is deactivated." : "The webhook was switched off.",
                retry: false);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        delivery.Claim(clock.UtcNow, Lease);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await SendAndRecordAsync(webhook, delivery, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Sends now and records the result on the delivery (not saved).</summary>
    public async Task SendAndRecordAsync(Webhook webhook, WebhookDelivery delivery, CancellationToken cancellationToken, bool retry = true) {
        var result = await sender.SendAsync(webhook, delivery, cancellationToken);
        if (result.Succeeded) {
            delivery.RecordSuccess(clock.UtcNow, result.StatusCode ?? 200);
        } else {
            delivery.RecordFailure(clock.UtcNow, result.StatusCode, result.Error ?? "The call failed.", retry && result.Retry);
        }
    }
}

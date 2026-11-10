using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Webhooks;

/// <summary>
/// Queues an event for every switched-on webhook that listens to it. Call it before the
/// handler's SaveChanges: the deliveries are saved together with the change, so an event is
/// never sent for something that didn't happen, or lost for something that did. Sending
/// happens afterwards, in the background, so a slow or broken receiver never slows the app.
/// </summary>
public class WebhookQueue(IUnitOfWork unitOfWork, IDateTimeProvider clock) {
    public async Task QueueAsync(
        string eventType, string summary, Func<Guid, DateTime, object> payload, CancellationToken cancellationToken
    ) {
        var webhooks = (await unitOfWork.Webhooks.GetAllAsync(cancellationToken)).Where(w => w.Wants(eventType)).ToList();
        if (webhooks.Count == 0) {
            return;
        }

        var eventId = Guid.NewGuid();
        var now = clock.UtcNow;
        var body = WebhookPayloads.Serialize(payload(eventId, now));
        foreach (var webhook in webhooks) {
            await unitOfWork.Webhooks.AddDeliveryAsync(
                WebhookDelivery.Create(webhook.Id, eventId, eventType, summary, body, now), cancellationToken);
        }
    }

    /// <summary>Queues the event matching the request's current status (requested, approved, rejected, cancelled).</summary>
    /// <param name="days">The days just booked, when the request was approved; sent along so receivers know the hours.</param>
    public Task LeaveChangedAsync(
        LeaveRequest request, User requester, HourType type, User actor, IReadOnlyList<LeaveDay>? days,
        CancellationToken cancellationToken
    ) {
        var eventType = request.Status switch {
            LeaveRequestStatus.Pending => WebhookEvents.LeaveRequested,
            LeaveRequestStatus.Approved => WebhookEvents.LeaveApproved,
            LeaveRequestStatus.Rejected => WebhookEvents.LeaveRejected,
            _ => WebhookEvents.LeaveCancelled
        };
        return QueueAsync(eventType, WebhookPayloads.LeaveSummary(requester, request),
            (id, at) => WebhookPayloads.LeaveEvent(id, eventType, at, actor, request, requester, type, days),
            cancellationToken);
    }
}

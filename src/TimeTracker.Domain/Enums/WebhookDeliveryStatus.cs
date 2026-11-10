namespace TimeTracker.Domain.Enums;

public enum WebhookDeliveryStatus {
    /// <summary>Waiting for its first try, or for the next retry.</summary>
    Pending,
    Succeeded,
    /// <summary>Every try failed, or the webhook was switched off or removed in the meantime.</summary>
    Failed
}

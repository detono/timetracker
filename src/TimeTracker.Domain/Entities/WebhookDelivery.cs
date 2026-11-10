using TimeTracker.Domain.Enums;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// One event to send to one webhook, saved together with the change it's about. That makes
/// it both the queue (nothing is lost when the server restarts or the receiver is down) and
/// the history employers see under "Recent calls".
/// </summary>
public class WebhookDelivery : TenantOwnedEntity {
    public const int MaxSummaryLength = 300;
    public const int MaxErrorLength = 1000;

    /// <summary>Waits before each retry; after the last one fails, the delivery has failed.</summary>
    public static readonly IReadOnlyList<TimeSpan> RetryDelays = new[] {
        TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(2), TimeSpan.FromHours(6)
    };

    public static int MaxAttempts => RetryDelays.Count + 1;

    public Guid WebhookId { get; private set; }

    /// <summary>The same for every webhook that gets this event, and on every retry, so receivers can skip duplicates.</summary>
    public Guid EventId { get; private set; }

    public string EventType { get; private set; } = default!;

    /// <summary>What the event is about, for people reading the list, e.g. "Jonathan Peeters · 16–20 Nov 2026".</summary>
    public string Summary { get; private set; } = default!;

    /// <summary>The JSON body, exactly as sent.</summary>
    public string Payload { get; private set; } = default!;

    public WebhookDeliveryStatus Status { get; private set; } = WebhookDeliveryStatus.Pending;
    public int Attempts { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public int? LastStatusCode { get; private set; }
    public string? LastError { get; private set; }

    private WebhookDelivery() {
        // EF Core
    }

    public static WebhookDelivery Create(
        Guid webhookId, Guid eventId, string eventType, string summary, string payload, DateTime nowUtc
    ) => new() {
        WebhookId = webhookId,
        EventId = eventId,
        EventType = eventType,
        Summary = summary.Length > MaxSummaryLength ? summary[..MaxSummaryLength] : summary,
        Payload = payload,
        NextAttemptAtUtc = nowUtc,
        CreatedAtUtc = nowUtc
    };

    /// <summary>Pushes the next try back while one is running, so a second worker won't pick it up too.</summary>
    public void Claim(DateTime nowUtc, TimeSpan lease) {
        NextAttemptAtUtc = nowUtc + lease;
        MarkUpdated();
    }

    public void RecordSuccess(DateTime nowUtc, int statusCode) {
        Attempts++;
        Status = WebhookDeliveryStatus.Succeeded;
        LastAttemptAtUtc = nowUtc;
        LastStatusCode = statusCode;
        LastError = null;
        NextAttemptAtUtc = null;
        MarkUpdated();
    }

    /// <summary>Schedules the next retry, or gives up after <see cref="MaxAttempts"/>.</summary>
    public void RecordFailure(DateTime nowUtc, int? statusCode, string error, bool retry = true) {
        Attempts++;
        LastAttemptAtUtc = nowUtc;
        LastStatusCode = statusCode;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        if (retry && Attempts < MaxAttempts) {
            NextAttemptAtUtc = nowUtc + RetryDelays[Attempts - 1];
        } else {
            Status = WebhookDeliveryStatus.Failed;
            NextAttemptAtUtc = null;
        }

        MarkUpdated();
    }

    /// <summary>"Send again": queues it for right away, with a fresh set of retries.</summary>
    public void Retry(DateTime nowUtc) {
        Status = WebhookDeliveryStatus.Pending;
        Attempts = 0;
        NextAttemptAtUtc = nowUtc;
        MarkUpdated();
    }
}

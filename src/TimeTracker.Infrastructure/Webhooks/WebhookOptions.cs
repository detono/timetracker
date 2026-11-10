namespace TimeTracker.Infrastructure.Webhooks;

/// <summary>Installation-wide webhook settings (the <c>Webhooks</c> section, e.g. <c>Webhooks__AllowInternalHosts</c>).</summary>
public class WebhookOptions {
    public const string SectionName = "Webhooks";

    /// <summary>
    /// By default webhooks may only call public addresses: otherwise an employer could point one
    /// at the database or other services inside this server's network. Set to true when the
    /// receiver (say, your own n8n) runs on your own network.
    /// </summary>
    public bool AllowInternalHosts { get; set; }

    /// <summary>Sends queued calls in the background. Tests switch it off and send by hand.</summary>
    public bool BackgroundSending { get; set; } = true;

    /// <summary>How long finished calls stay in the "Recent calls" history.</summary>
    public int HistoryDays { get; set; } = 30;
}

namespace TimeTracker.Application.Webhooks;

/// <summary>
/// Every event a webhook can listen to. Adding one: add its name here (the settings page
/// lists these), queue it with <see cref="WebhookQueue"/> where it happens, and add a test.
/// </summary>
public static class WebhookEvents {
    public const string LeaveRequested = "leave.requested";
    public const string LeaveApproved = "leave.approved";
    public const string LeaveRejected = "leave.rejected";
    public const string LeaveCancelled = "leave.cancelled";

    /// <summary>Only sent by "Send test"; can't be chosen.</summary>
    public const string Test = "webhook.test";

    public static readonly IReadOnlyList<string> All = new[] {
        LeaveRequested, LeaveApproved, LeaveRejected, LeaveCancelled
    };
}

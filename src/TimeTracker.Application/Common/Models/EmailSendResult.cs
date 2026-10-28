namespace TimeTracker.Application.Common.Models;

public enum EmailSendStatus
{
    Sent = 0,

    /// <summary>The tenant has no mail settings, or sending is switched off. Not an error.</summary>
    NotConfigured = 1,

    /// <summary>Delivery was attempted and failed; <see cref="EmailSendResult.Error"/> says why.</summary>
    Failed = 2
}

public record EmailSendResult(EmailSendStatus Status, string? Error = null)
{
    public static EmailSendResult Sent() => new(EmailSendStatus.Sent);
    public static EmailSendResult NotConfigured() => new(EmailSendStatus.NotConfigured);
    public static EmailSendResult Failed(string error) => new(EmailSendStatus.Failed, error);
}

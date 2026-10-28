using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Common.Interfaces;

/// <summary>
/// Sends email on behalf of the caller's tenant, using that tenant's own mail settings.
/// Meant for notifications: it never throws for delivery problems. If the tenant has no
/// mail settings or has sending switched off, nothing is sent and
/// <see cref="EmailSendStatus.NotConfigured"/> is returned; delivery failures are logged
/// and returned as <see cref="EmailSendStatus.Failed"/>.
/// </summary>
public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

using Microsoft.Extensions.Logging;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Email;

public class EmailSender(
    ITenantMailSettingsRepository mailSettings,
    IMailDispatcher dispatcher,
    ICurrentUserService currentUser,
    ILogger<EmailSender> logger
) : IEmailSender {
    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default) {
        var settings = await mailSettings.GetForCurrentTenantAsync(cancellationToken);
        if (settings is null || !settings.IsEnabled) {
            logger.LogInformation(
                "Email \"{Subject}\" not sent: tenant {TenantId} has no enabled mail settings.",
                message.Subject, currentUser.TenantId);
            return EmailSendResult.NotConfigured();
        }

        try {
            await dispatcher.DispatchAsync(settings, message, cancellationToken);
        } catch (EmailDeliveryException ex) {
            logger.LogWarning(ex,
                "Email \"{Subject}\" for tenant {TenantId} could not be sent via {Provider}: {Reason}",
                message.Subject, currentUser.TenantId, settings.Provider, ex.Message);
            return EmailSendResult.Failed(ex.Message);
        }

        logger.LogInformation("Email \"{Subject}\" sent for tenant {TenantId} via {Provider} to {RecipientCount} recipient(s).",
            message.Subject, currentUser.TenantId, settings.Provider, message.To.Count);
        return EmailSendResult.Sent();
    }
}

using System.Security.Cryptography;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Infrastructure.Email;

/// <summary>Picks the transport for the tenant's chosen provider at send time.</summary>
public class MailDispatcher(SmtpMailTransport smtp, GraphMailTransport graph) : IMailDispatcher {
    public Task DispatchAsync(TenantMailSettings settings, EmailMessage message, CancellationToken cancellationToken = default) {
        if (message.To.Count == 0 || message.To.Any(string.IsNullOrWhiteSpace)) {
            throw new EmailDeliveryException("The email has no valid recipient.");
        }

        return settings.Provider switch {
            MailProvider.Smtp => smtp.SendAsync(settings, message, cancellationToken),
            MailProvider.MicrosoftGraph => graph.SendAsync(settings, message, cancellationToken),
            _ => throw new EmailDeliveryException($"Unsupported mail provider '{settings.Provider}'.")
        };
    }

    /// <summary>Decrypts a stored secret, turning a lost key ring into a message the employer can act on.</summary>
    internal static string UnprotectSecret(ISecretProtector protector, string protectedSecret, string what) {
        try {
            return protector.Unprotect(protectedSecret);
        } catch (CryptographicException ex) {
            throw new EmailDeliveryException(
                $"The stored {what} can no longer be decrypted (the server's encryption keys changed). Enter it again and save.", ex);
        }
    }
}

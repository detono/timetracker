using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Common.Interfaces;

/// <summary>
/// Delivers one message with the given settings, regardless of whether they're enabled.
/// Used by <see cref="IEmailSender"/> and by the "send test email" feature.
/// </summary>
public interface IMailDispatcher
{
    /// <exception cref="EmailDeliveryException">With a message that can be shown to the employer.</exception>
    Task DispatchAsync(TenantMailSettings settings, EmailMessage message, CancellationToken cancellationToken = default);
}

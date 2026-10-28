using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.MailSettings.Commands.SendTestEmail;

/// <summary>
/// Employer-only: sends a test email with the saved settings to the calling employer's own
/// address. Works even while sending is switched off, so settings can be checked first.
/// Returns the delivery error as the failure message.
/// </summary>
public record SendTestEmailCommand : IRequest<Result>;

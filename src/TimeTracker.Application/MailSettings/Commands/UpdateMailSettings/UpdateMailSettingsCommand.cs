using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.MailSettings.Dtos;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Application.MailSettings.Commands.UpdateMailSettings;

/// <summary>
/// Employer-only: saves the company's mail settings. Only the chosen provider's fields
/// are used. An empty <see cref="SmtpPassword"/> or <see cref="GraphClientSecret"/> keeps
/// the secret that is already stored.
/// </summary>
public record UpdateMailSettingsCommand(
    MailProvider Provider,
    bool IsEnabled,
    string FromAddress,
    string? FromName,
    string? SmtpHost,
    int SmtpPort,
    SmtpSecurity SmtpSecurity,
    string? SmtpUsername,
    string? SmtpPassword,
    string? GraphTenantId,
    string? GraphClientId,
    string? GraphClientSecret,
    string? GraphMailbox
) : IRequest<Result<MailSettingsDto>>;

using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Application.MailSettings.Dtos;

/// <summary>A tenant's mail settings without secrets: only whether each secret is set.</summary>
public record MailSettingsDto(
    bool IsConfigured,
    bool IsEnabled,
    MailProvider Provider,
    string FromAddress,
    string? FromName,
    string? SmtpHost,
    int SmtpPort,
    SmtpSecurity SmtpSecurity,
    string? SmtpUsername,
    bool HasSmtpPassword,
    string? GraphTenantId,
    string? GraphClientId,
    string? GraphMailbox,
    bool HasGraphClientSecret)
{
    public static MailSettingsDto NotConfigured() => new(
        false, false, MailProvider.Smtp, "", null, null, 587, SmtpSecurity.StartTls, null, false,
        null, null, null, false);

    public static MailSettingsDto From(TenantMailSettings s) => new(
        true, s.IsEnabled, s.Provider, s.FromAddress, s.FromName,
        s.SmtpHost, s.SmtpPort, s.SmtpSecurity, s.SmtpUsername, s.SmtpPasswordProtected is not null,
        s.GraphTenantId, s.GraphClientId, s.GraphMailbox, s.GraphClientSecretProtected is not null);
}

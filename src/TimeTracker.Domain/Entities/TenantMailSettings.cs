using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// How a tenant sends email, one record per tenant. Settings for both providers are kept,
/// so switching provider and back doesn't lose what was filled in. Secrets (SMTP password,
/// Graph client secret) are stored already encrypted; this entity never sees them in clear text.
/// </summary>
public class TenantMailSettings : TenantOwnedEntity {
    public MailProvider Provider { get; private set; } = MailProvider.Smtp;
    public bool IsEnabled { get; private set; }

    public string FromAddress { get; private set; } = default!;
    public string? FromName { get; private set; }

    public string? SmtpHost { get; private set; }
    public int SmtpPort { get; private set; } = 587;
    public SmtpSecurity SmtpSecurity { get; private set; } = SmtpSecurity.StartTls;
    public string? SmtpUsername { get; private set; }
    public string? SmtpPasswordProtected { get; private set; }

    /// <summary>The Microsoft Entra (Azure AD) tenant id or domain of the company's Microsoft 365.</summary>
    public string? GraphTenantId { get; private set; }
    public string? GraphClientId { get; private set; }
    public string? GraphClientSecretProtected { get; private set; }

    /// <summary>The Microsoft 365 mailbox (user principal name or address) mail is sent from.</summary>
    public string? GraphMailbox { get; private set; }

    private TenantMailSettings() {
        // EF Core
    }

    public static TenantMailSettings Create(string fromAddress, string? fromName) {
        var settings = new TenantMailSettings();
        settings.SetSender(fromAddress, fromName);
        return settings;
    }

    public void SetSender(string fromAddress, string? fromName) {
        if (string.IsNullOrWhiteSpace(fromAddress)) {
            throw new DomainException("A sender address is required.");
        }

        FromAddress = fromAddress.Trim();
        FromName = string.IsNullOrWhiteSpace(fromName) ? null : fromName.Trim();
        MarkUpdated();
    }

    public void SetEnabled(bool isEnabled) {
        IsEnabled = isEnabled;
        MarkUpdated();
    }

    /// <summary>
    /// Switches to SMTP. A null <paramref name="protectedPassword"/> keeps the stored one;
    /// an empty username means the relay needs no login, so any stored password is dropped.
    /// </summary>
    public void UseSmtp(string host, int port, SmtpSecurity security, string? username, string? protectedPassword) {
        if (string.IsNullOrWhiteSpace(host)) {
            throw new DomainException("An SMTP host is required.");
        }

        if (port is < 1 or > 65535) {
            throw new DomainException("The SMTP port must be between 1 and 65535.");
        }

        Provider = MailProvider.Smtp;
        SmtpHost = host.Trim();
        SmtpPort = port;
        SmtpSecurity = security;

        if (string.IsNullOrWhiteSpace(username)) {
            SmtpUsername = null;
            SmtpPasswordProtected = null;
        } else {
            SmtpUsername = username.Trim();
            if (protectedPassword is not null) {
                SmtpPasswordProtected = protectedPassword;
            }
        }

        MarkUpdated();
    }

    /// <summary>Switches to Microsoft Graph. A null <paramref name="protectedClientSecret"/> keeps the stored one.</summary>
    public void UseMicrosoftGraph(string tenantId, string clientId, string mailbox, string? protectedClientSecret) {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(mailbox)) {
            throw new DomainException("Microsoft Graph needs a tenant id, a client id and a mailbox.");
        }

        if (protectedClientSecret is null && GraphClientSecretProtected is null) {
            throw new DomainException("A client secret is required.");
        }

        Provider = MailProvider.MicrosoftGraph;
        GraphTenantId = tenantId.Trim();
        GraphClientId = clientId.Trim();
        GraphMailbox = mailbox.Trim();
        if (protectedClientSecret is not null) {
            GraphClientSecretProtected = protectedClientSecret;
        }

        MarkUpdated();
    }
}

namespace TimeTracker.Domain.Enums;

/// <summary>How a tenant's outgoing email is delivered.</summary>
public enum MailProvider
{
    /// <summary>Any SMTP server or relay (e.g. Brevo, Mailgun, the company's own mail server).</summary>
    Smtp = 0,

    /// <summary>Microsoft 365 through the Graph API, for tenants where password-based SMTP is turned off.</summary>
    MicrosoftGraph = 1
}

namespace TimeTracker.Domain.Enums;

/// <summary>How the SMTP connection is secured.</summary>
public enum SmtpSecurity
{
    /// <summary>Connect in plain text, then upgrade with STARTTLS (required). Usually port 587.</summary>
    StartTls = 0,

    /// <summary>TLS from the first byte ("implicit TLS"). Usually port 465.</summary>
    SslOnConnect = 1,

    /// <summary>No encryption. Only for a trusted relay on a private network.</summary>
    None = 2
}

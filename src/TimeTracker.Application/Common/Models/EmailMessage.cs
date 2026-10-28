namespace TimeTracker.Application.Common.Models;

/// <summary>
/// An email to send. <paramref name="TextBody"/> is the plain-text alternative for mail
/// clients that don't show HTML; Microsoft Graph only sends the HTML body.
/// </summary>
public record EmailMessage(
    IReadOnlyList<string> To,
    string Subject,
    string HtmlBody,
    string? TextBody = null);

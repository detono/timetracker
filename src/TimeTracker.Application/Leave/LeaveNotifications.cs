using System.Globalization;
using TimeTracker.Application.Common.Email;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Localization;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Leave;

/// <summary>
/// Emails about leave requests, each in the recipient's own language. Sending never fails
/// the request itself: when the company has no email set up, people still see everything
/// in the app.
/// </summary>
public class LeaveNotifications(IUnitOfWork unitOfWork, IEmailSender emailSender, IAppBranding branding) {
    /// <summary>
    /// Tells the requester's supervisor, or every active employer if they have none. With
    /// <c>APP_URL</c> set, the email has Approve and Reject buttons that open the request on the Leave page, where the approver signs in (if
    /// needed) and confirms.
    /// </summary>
    public async Task RequestSubmittedAsync(
        LeaveRequest request, User requester, HourType hourType, CancellationToken cancellationToken
    ) {
        var recipients = await ApproversOfAsync(requester, cancellationToken);
        var name = $"{requester.FirstName} {requester.LastName}";

        // One email per language, so everyone reads it in their own.
        foreach (var group in recipients.GroupBy(r => EmailTexts.For(r.Language).Language)) {
            var texts = EmailTexts.For(group.Key);
            var period = Period(request, texts);
            var type = TypeName(hourType, texts.Language);
            var hours = request.Hours.ToString("0.##", texts.Culture);
            var body = texts.Get("leaveSubmitted.body", ("name", name), ("type", type), ("period", period), ("hours", hours));
            var comment = request.Comment is null ? null : texts.Get("comment", ("comment", request.Comment));

            var content = EmailHtml.Paragraphs(texts.Get("leaveSubmitted.intro", ("name", name)))
                          + Details(texts, type, period, hours, request.Comment);
            string text;
            if (branding.AppUrl is { } appUrl) {
                var approve = texts.Get("leaveSubmitted.approve");
                var reject = texts.Get("leaveSubmitted.reject");
                var approveUrl = $"{appUrl}/leave?review={request.Id}&decision=approve";
                var rejectUrl = $"{appUrl}/leave?review={request.Id}&decision=reject";
                content += EmailHtml.Buttons(
                    new EmailButton(approve, approveUrl, branding.ColorPrimary, branding.ColorOnPrimary),
                    new EmailButton(reject, rejectUrl, branding.ColorSecondary, branding.ColorOnSecondary));
                text = Text(body, comment, $"{approve}: {approveUrl}\n{reject}: {rejectUrl}");
            } else {
                var action = texts.Get("leaveSubmitted.action", ("app", branding.Title));
                content += EmailHtml.Paragraphs(action);
                text = Text(body, comment, action);
            }

            await emailSender.SendAsync(new EmailMessage(
                group.Select(r => r.Email).ToList(),
                texts.Get("leaveSubmitted.subject", ("name", name), ("period", period)),
                EmailHtml.Document(branding, texts.Get("leaveSubmitted.heading"), content, Footer(texts)),
                text), cancellationToken);
        }
    }

    /// <summary>Tells the requester their request was approved, rejected or cancelled by someone else.</summary>
    public async Task RequestReviewedAsync(
        LeaveRequest request, User requester, User reviewer, HourType hourType, CancellationToken cancellationToken
    ) {
        if (requester.Id == reviewer.Id || !requester.IsActive) {
            return;
        }

        var key = request.Status switch {
            LeaveRequestStatus.Approved => "leaveApproved",
            LeaveRequestStatus.Rejected => "leaveRejected",
            _ => "leaveCancelled"
        };
        var texts = EmailTexts.For(requester.Language);
        var period = Period(request, texts);
        var type = TypeName(hourType, texts.Language);
        var greeting = texts.Get("greeting", ("firstName", requester.FirstName));
        var body = texts.Get($"{key}.body",
            ("reviewer", $"{reviewer.FirstName} {reviewer.LastName}"), ("type", type), ("period", period));
        var comment = request.ReviewComment is null ? null : texts.Get("comment", ("comment", request.ReviewComment));

        var content = EmailHtml.Paragraphs(greeting, body)
                      + Details(texts, type, period, request.Hours.ToString("0.##", texts.Culture), null);
        if (request.ReviewComment is not null) {
            content += EmailHtml.Paragraphs(comment);
        }

        if (branding.AppUrl is { } appUrl) {
            content += EmailHtml.Buttons(new EmailButton(
                texts.Get("openApp", ("app", branding.Title)), $"{appUrl}/leave", branding.ColorPrimary, branding.ColorOnPrimary));
        }

        await emailSender.SendAsync(new EmailMessage(
            new[] { requester.Email },
            texts.Get($"{key}.subject", ("period", period)),
            EmailHtml.Document(branding, texts.Get($"{key}.heading"), content, Footer(texts)),
            Text(greeting, body, comment)), cancellationToken);
    }

    private async Task<IReadOnlyList<User>> ApproversOfAsync(User requester, CancellationToken cancellationToken) {
        if (requester.SupervisorId is { } supervisorId) {
            var supervisor = await unitOfWork.Users.GetByIdAsync(supervisorId, cancellationToken);
            if (supervisor is { IsActive: true }) {
                return new[] { supervisor };
            }
        }

        return (await unitOfWork.Users.GetAllAsync(cancellationToken))
            .Where(u => u.Role == UserRole.Employer && u.IsActive && u.Id != requester.Id)
            .ToList();
    }

    private string Details(EmailTexts texts, string type, string period, string hours, string? comment) =>
        EmailHtml.Details(branding.ColorPrimary,
            (texts.Get("label.type"), type),
            (texts.Get("label.period"), period),
            (texts.Get("label.hours"), hours),
            (texts.Get("label.comment"), comment));

    private string Footer(EmailTexts texts) => texts.Get("footer", ("app", branding.Title));

    private static string Period(LeaveRequest request, EmailTexts texts) {
        string Day(DateOnly date, DayPart part) =>
            date.ToString("ddd d MMM yyyy", texts.Culture) + part switch {
                DayPart.Morning => $" ({texts.Get("part.morning")})",
                DayPart.Afternoon => $" ({texts.Get("part.afternoon")})",
                DayPart.Hours when request is { StartTime: { } from, EndTime: { } to } =>
                    $", {from.ToString("HH:mm", CultureInfo.InvariantCulture)}–{to.ToString("HH:mm", CultureInfo.InvariantCulture)}",
                _ => ""
            };

        return request.StartDate == request.EndDate
            ? Day(request.StartDate, request.FirstDayPart)
            : texts.Get("period.range",
                ("start", Day(request.StartDate, request.FirstDayPart)),
                ("end", Day(request.EndDate, request.LastDayPart)));
    }

    /// <summary>The hour type's name in the email's language, else English, else any.</summary>
    private static string TypeName(HourType hourType, string language) =>
        hourType.LocalizedNames.TryGetValue(language, out var name) ? name
        : hourType.LocalizedNames.TryGetValue(EmailTexts.DefaultLanguage, out var en) ? en
        : hourType.LocalizedNames.Values.FirstOrDefault() ?? "";

    private static string Text(params string?[] paragraphs) =>
        string.Join("\n\n", paragraphs.Where(p => p is not null));
}

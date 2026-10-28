using System.Net;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Leave;

/// <summary>
/// Emails about leave requests. Sending never fails the request itself: when the company
/// has no email set up, people still see everything in the app.
/// </summary>
public class LeaveNotifications(IUnitOfWork unitOfWork, IEmailSender emailSender) {
    /// <summary>Tells the requester's supervisor, or every active employer if they have none.</summary>
    public async Task RequestSubmittedAsync(
        LeaveRequest request, User requester, string hourTypeName, CancellationToken cancellationToken
    ) {
        var recipients = await ApproversOfAsync(requester, cancellationToken);
        if (recipients.Count == 0) {
            return;
        }

        var name = $"{requester.FirstName} {requester.LastName}";
        var period = Period(request);
        var comment = request.Comment is null ? "" : $"\n\nComment: {request.Comment}";

        await emailSender.SendAsync(new EmailMessage(
            recipients,
            $"Leave request from {name}: {period}",
            $"<p><strong>{Encode(name)}</strong> requested <strong>{Encode(hourTypeName)}</strong> " +
            $"from {Encode(period)} ({request.Hours:0.##} hours).</p>" +
            (request.Comment is null ? "" : $"<p>Comment: {Encode(request.Comment)}</p>") +
            "<p>Open TimeTracker and go to Leave to approve or reject it.</p>",
            $"{name} requested {hourTypeName} from {period} ({request.Hours:0.##} hours).{comment}\n\n" +
            "Open TimeTracker and go to Leave to approve or reject it."), cancellationToken);
    }

    /// <summary>Tells the requester their request was approved, rejected or cancelled by someone else.</summary>
    public async Task RequestReviewedAsync(
        LeaveRequest request, User requester, User reviewer, string hourTypeName, CancellationToken cancellationToken
    ) {
        if (requester.Id == reviewer.Id || !requester.IsActive) {
            return;
        }

        var outcome = request.Status switch {
            LeaveRequestStatus.Approved => "approved",
            LeaveRequestStatus.Rejected => "rejected",
            _ => "cancelled"
        };
        var reviewerName = $"{reviewer.FirstName} {reviewer.LastName}";
        var period = Period(request);
        var comment = request.ReviewComment is null ? "" : $"\n\nComment: {request.ReviewComment}";

        await emailSender.SendAsync(new EmailMessage(
            new[] { requester.Email },
            $"Your leave request was {outcome}: {period}",
            $"<p>Hi {Encode(requester.FirstName)},</p>" +
            $"<p>{Encode(reviewerName)} {outcome} your request for <strong>{Encode(hourTypeName)}</strong> " +
            $"from {Encode(period)}.</p>" +
            (request.ReviewComment is null ? "" : $"<p>Comment: {Encode(request.ReviewComment)}</p>"),
            $"Hi {requester.FirstName},\n\n{reviewerName} {outcome} your request for {hourTypeName} from {period}.{comment}"),
            cancellationToken);
    }

    private async Task<IReadOnlyList<string>> ApproversOfAsync(User requester, CancellationToken cancellationToken) {
        if (requester.SupervisorId is { } supervisorId) {
            var supervisor = await unitOfWork.Users.GetByIdAsync(supervisorId, cancellationToken);
            if (supervisor is { IsActive: true }) {
                return new[] { supervisor.Email };
            }
        }

        return (await unitOfWork.Users.GetAllAsync(cancellationToken))
            .Where(u => u.Role == UserRole.Employer && u.IsActive && u.Id != requester.Id)
            .Select(u => u.Email)
            .ToList();
    }

    private static string Period(LeaveRequest request) =>
        request.StartDate == request.EndDate
            ? request.StartDate.ToString("ddd d MMM yyyy") + Part(request.FirstDayPart)
            : $"{request.StartDate:ddd d MMM yyyy}{Part(request.FirstDayPart)} to {request.EndDate:ddd d MMM yyyy}{Part(request.LastDayPart)}";

    private static string Part(DayPart part) => part switch {
        DayPart.Morning => " (morning)",
        DayPart.Afternoon => " (afternoon)",
        _ => ""
    };

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}

using System.Globalization;
using System.Text.Json;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.AuditLog;

/// <summary>
/// Writes to the company's activity log. The entry is saved together with the change itself
/// (the handler's SaveChanges), so the log never shows something that didn't happen, or misses
/// something that did.
/// </summary>
public class AuditTrail(IUnitOfWork unitOfWork, ICurrentUserService currentUser) {
    public async Task RecordAsync(
        AuditAction action, User? subject, IReadOnlyDictionary<string, string>? details, CancellationToken cancellationToken
    ) {
        var entry = AuditEntry.Create(
            currentUser.UserId == Guid.Empty ? null : currentUser.UserId,
            string.IsNullOrWhiteSpace(currentUser.FullName) ? "Unknown" : currentUser.FullName,
            currentUser.Role,
            action,
            subject?.Id,
            subject is null ? null : $"{subject.FirstName} {subject.LastName}",
            details is null || details.Count == 0 ? null : JsonSerializer.Serialize(details));
        await unitOfWork.AuditEntries.AddAsync(entry, cancellationToken);
    }

    /// <summary>A time entry in a language-neutral form, e.g. "2026-10-05 09:00–17:00, 30 min break, Work".</summary>
    public static string Describe(TimeEntry entry, HourType? hourType) {
        var type = hourType is null ? null
            : hourType.LocalizedNames.TryGetValue("en", out var en) ? en
            : hourType.LocalizedNames.Values.FirstOrDefault();
        var text = $"{entry.WorkDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} " +
                   $"{entry.StartTime:HH\\:mm}–{entry.EndTime:HH\\:mm}";
        if (entry.BreakMinutes > 0) text += $", {entry.BreakMinutes} min break";
        return type is null ? text : $"{text}, {type}";
    }
}

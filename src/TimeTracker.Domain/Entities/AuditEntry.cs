using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// One line in a company's activity log: who did what, to whom, and when. Names are copied
/// in, so the log still reads right after someone is renamed or leaves. Entries are never
/// changed or deleted.
/// </summary>
public class AuditEntry : TenantOwnedEntity {
    public Guid? ActorUserId { get; private set; }
    public string ActorName { get; private set; } = default!;
    public UserRole ActorRole { get; private set; }
    public AuditAction Action { get; private set; }
    public Guid? SubjectUserId { get; private set; }
    public string? SubjectName { get; private set; }

    /// <summary>Extra facts as JSON, e.g. the month or the hours before and after a change.</summary>
    public string? Details { get; private set; }

    private AuditEntry() {
        // EF Core
    }

    public static AuditEntry Create(
        Guid? actorUserId, string actorName, UserRole actorRole, AuditAction action,
        Guid? subjectUserId, string? subjectName, string? details
    ) {
        if (string.IsNullOrWhiteSpace(actorName)) {
            throw new DomainException("Who did it is required.");
        }

        return new AuditEntry {
            ActorUserId = actorUserId,
            ActorName = actorName.Trim(),
            ActorRole = actorRole,
            Action = action,
            SubjectUserId = subjectUserId,
            SubjectName = subjectName,
            Details = details
        };
    }
}

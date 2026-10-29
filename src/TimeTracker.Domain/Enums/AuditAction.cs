namespace TimeTracker.Domain.Enums;

/// <summary>What an audit entry records. Stored by name, so values can be added freely.</summary>
public enum AuditAction
{
    MonthLocked,
    MonthUnlocked,
    /// <summary>Hours added, changed or removed by someone other than the person they belong to.</summary>
    TimeEntryCreated,
    TimeEntryUpdated,
    TimeEntryDeleted,
    /// <summary>A temporary password set by an employer or platform admin.</summary>
    PasswordReset,
    UserDeactivated,
    UserActivated
}

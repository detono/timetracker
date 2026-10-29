namespace TimeTracker.Application.Common.Interfaces;

/// <summary>Checked on every authenticated request, so signing someone out takes effect right away.</summary>
public interface ISessionValidator
{
    /// <summary>
    /// False when the user or their company was deactivated, when the sign-in is older than
    /// their last password change or reset, or when the token's role or company don't match
    /// the account (so even a token signed with a leaked secret can't claim more rights).
    /// </summary>
    Task<bool> IsValidAsync(
        Guid userId, int sessionVersion, string? role, Guid? tenantId, CancellationToken cancellationToken = default);
}

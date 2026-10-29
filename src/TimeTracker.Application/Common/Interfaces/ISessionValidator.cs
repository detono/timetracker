namespace TimeTracker.Application.Common.Interfaces;

/// <summary>Checked on every authenticated request, so signing someone out takes effect right away.</summary>
public interface ISessionValidator
{
    /// <summary>
    /// False when the user or their company was deactivated, or when the sign-in is older than
    /// their last password change or reset.
    /// </summary>
    Task<bool> IsValidAsync(Guid userId, int sessionVersion, CancellationToken cancellationToken = default);
}

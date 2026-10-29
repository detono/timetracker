using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface ITenantMailSettingsRepository
{
    /// <summary>The caller's tenant's mail settings, or null if none were saved yet.</summary>
    Task<TenantMailSettings?> GetForCurrentTenantAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Whether any active company, across the whole installation, has email switched on.
    /// Used before sign-in, to decide whether to offer "forgot password".
    /// </summary>
    Task<bool> AnyEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TenantMailSettings settings, CancellationToken cancellationToken = default);
}

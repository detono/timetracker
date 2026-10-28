using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface ITenantMailSettingsRepository
{
    /// <summary>The caller's tenant's mail settings, or null if none were saved yet.</summary>
    Task<TenantMailSettings?> GetForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TenantMailSettings settings, CancellationToken cancellationToken = default);
}

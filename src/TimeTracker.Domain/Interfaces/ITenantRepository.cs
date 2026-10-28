using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

/// <summary>Tenant administration. Used by platform admins and by login, never scoped to a tenant.</summary>
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Number of users per tenant id, across all tenants.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetUserCountsAsync(CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Whether another tenant than <paramref name="tenantId"/> already uses this name.</summary>
    Task<bool> NameUsedByOtherAsync(string name, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    void Update(Tenant tenant);
}

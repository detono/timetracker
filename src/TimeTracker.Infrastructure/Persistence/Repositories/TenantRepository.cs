using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class TenantRepository(ApplicationDbContext context) : ITenantRepository {
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Tenants.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetUserCountsAsync(CancellationToken cancellationToken = default) =>
        await context.Users.IgnoreQueryFilters()
            .Where(u => u.TenantId != null)
            .GroupBy(u => u.TenantId!.Value)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) {
        var normalized = name.Trim().ToLower();
        return context.Tenants.AnyAsync(t => t.Name.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> NameUsedByOtherAsync(string name, Guid tenantId, CancellationToken cancellationToken = default) {
        var normalized = name.Trim().ToLower();
        return context.Tenants.AnyAsync(t => t.Name.ToLower() == normalized && t.Id != tenantId, cancellationToken);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default) =>
        await context.Tenants.AddAsync(tenant, cancellationToken);

    public void Update(Tenant tenant) => context.Tenants.Update(tenant);
}

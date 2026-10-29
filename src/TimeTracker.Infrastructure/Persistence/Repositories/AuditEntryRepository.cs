using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class AuditEntryRepository(ApplicationDbContext context) : IAuditEntryRepository {
    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default) =>
        await context.AuditEntries.AddAsync(entry, cancellationToken);

    public async Task<IReadOnlyList<AuditEntry>> GetPageAsync(
        DateTime? beforeUtc, int take, CancellationToken cancellationToken = default
    ) {
        var query = context.AuditEntries.AsNoTracking();
        if (beforeUtc is { } before) {
            query = query.Where(e => e.CreatedAtUtc < before);
        }

        return await query.OrderByDescending(e => e.CreatedAtUtc).Take(take).ToListAsync(cancellationToken);
    }
}

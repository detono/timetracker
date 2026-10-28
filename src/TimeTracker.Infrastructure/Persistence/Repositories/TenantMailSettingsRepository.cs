using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class TenantMailSettingsRepository(ApplicationDbContext context) : ITenantMailSettingsRepository {
    // The tenant query filter narrows this to the caller's own record.
    public Task<TenantMailSettings?> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        context.MailSettings.FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(TenantMailSettings settings, CancellationToken cancellationToken = default) =>
        await context.MailSettings.AddAsync(settings, cancellationToken);
}

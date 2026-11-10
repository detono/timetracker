using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class WebhookRepository(ApplicationDbContext context) : IWebhookRepository {
    public async Task<IReadOnlyList<Webhook>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Webhooks.OrderBy(w => w.CreatedAtUtc).ToListAsync(cancellationToken);

    public Task<Webhook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Webhooks.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task AddAsync(Webhook webhook, CancellationToken cancellationToken = default) =>
        await context.Webhooks.AddAsync(webhook, cancellationToken);

    public async Task RemoveAsync(Webhook webhook, CancellationToken cancellationToken = default) {
        // Removed explicitly too, since not every database provider cascades.
        context.WebhookDeliveries.RemoveRange(
            await context.WebhookDeliveries.Where(d => d.WebhookId == webhook.Id).ToListAsync(cancellationToken));
        context.Webhooks.Remove(webhook);
    }

    public async Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default) =>
        await context.WebhookDeliveries.AddAsync(delivery, cancellationToken);

    public Task<WebhookDelivery?> GetDeliveryAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.WebhookDeliveries.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WebhookDelivery>> GetRecentDeliveriesAsync(int count, CancellationToken cancellationToken = default) =>
        await context.WebhookDeliveries.AsNoTracking()
            .OrderByDescending(d => d.CreatedAtUtc).ThenByDescending(d => d.Id)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(Guid Id, Guid TenantId)>> GetDueAcrossTenantsAsync(
        DateTime nowUtc, int count, CancellationToken cancellationToken = default
    ) {
        // Only ids leave this query; the sender reads each delivery again inside its own company.
        var due = await context.WebhookDeliveries.IgnoreQueryFilters().AsNoTracking()
            .Where(d => d.Status == WebhookDeliveryStatus.Pending && d.NextAttemptAtUtc <= nowUtc)
            .OrderBy(d => d.NextAttemptAtUtc)
            .Take(count)
            .Select(d => new { d.Id, d.TenantId })
            .ToListAsync(cancellationToken);
        return due.Select(d => (d.Id, d.TenantId)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetTenantsWithFinishedBeforeAsync(
        DateTime beforeUtc, CancellationToken cancellationToken = default
    ) =>
        await context.WebhookDeliveries.IgnoreQueryFilters()
            .Where(d => d.Status != WebhookDeliveryStatus.Pending && d.CreatedAtUtc < beforeUtc)
            .Select(d => d.TenantId).Distinct()
            .ToListAsync(cancellationToken);

    public async Task<int> DeleteFinishedBeforeAsync(DateTime beforeUtc, CancellationToken cancellationToken = default) {
        var old = await context.WebhookDeliveries
            .Where(d => d.Status != WebhookDeliveryStatus.Pending && d.CreatedAtUtc < beforeUtc)
            .Take(1000)
            .ToListAsync(cancellationToken);
        context.WebhookDeliveries.RemoveRange(old);
        return old.Count;
    }
}

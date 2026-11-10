using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IWebhookRepository {
    Task<IReadOnlyList<Webhook>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Webhook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Webhook webhook, CancellationToken cancellationToken = default);

    /// <summary>Removes the webhook and its delivery history.</summary>
    Task RemoveAsync(Webhook webhook, CancellationToken cancellationToken = default);

    Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);
    Task<WebhookDelivery?> GetDeliveryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The company's latest deliveries, newest first.</summary>
    Task<IReadOnlyList<WebhookDelivery>> GetRecentDeliveriesAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// For the background sender, across every company: deliveries whose next try is due,
    /// oldest first, as (delivery id, company id).
    /// </summary>
    Task<IReadOnlyList<(Guid Id, Guid TenantId)>> GetDueAcrossTenantsAsync(
        DateTime nowUtc, int count, CancellationToken cancellationToken = default);

    /// <summary>The companies that have finished deliveries created before <paramref name="beforeUtc"/>.</summary>
    Task<IReadOnlyList<Guid>> GetTenantsWithFinishedBeforeAsync(DateTime beforeUtc, CancellationToken cancellationToken = default);

    /// <summary>Removes the company's finished deliveries created before <paramref name="beforeUtc"/>.</summary>
    Task<int> DeleteFinishedBeforeAsync(DateTime beforeUtc, CancellationToken cancellationToken = default);
}

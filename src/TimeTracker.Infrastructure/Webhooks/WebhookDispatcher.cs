using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Webhooks;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Webhooks;

/// <summary>
/// Sends the queued webhook calls that are due, each inside its own company. The background
/// worker calls <see cref="SendDueAsync"/> every few seconds.
/// </summary>
public class WebhookDispatcher(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider clock,
    IOptions<WebhookOptions> options,
    ILogger<WebhookDispatcher> logger
) {
    private const int BatchSize = 20;

    /// <summary>Sends what's due now; returns how many deliveries were tried.</summary>
    public async Task<int> SendDueAsync(CancellationToken cancellationToken) {
        IReadOnlyList<(Guid Id, Guid TenantId)> due;
        using (var scope = scopeFactory.CreateScope()) {
            due = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Webhooks
                .GetDueAcrossTenantsAsync(clock.UtcNow, BatchSize, cancellationToken);
        }

        foreach (var (id, tenantId) in due) {
            try {
                using var scope = scopeFactory.CreateScope();
                using var tenant = scope.ServiceProvider.GetRequiredService<ITenantScope>().Enter(tenantId);
                await scope.ServiceProvider.GetRequiredService<WebhookDeliveryProcessor>().ProcessAsync(id, cancellationToken);
            } catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested) {
                // One broken delivery mustn't stop the others; it stays claimed and is tried again later.
                logger.LogError(ex, "Sending webhook delivery {DeliveryId} failed.", id);
            }
        }

        return due.Count;
    }

    /// <summary>Removes finished calls older than the history period.</summary>
    public async Task CleanUpAsync(CancellationToken cancellationToken) {
        var before = clock.UtcNow.AddDays(-Math.Max(1, options.Value.HistoryDays));
        IReadOnlyList<Guid> tenants;
        using (var scope = scopeFactory.CreateScope()) {
            tenants = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Webhooks
                .GetTenantsWithFinishedBeforeAsync(before, cancellationToken);
        }

        foreach (var tenantId in tenants) {
            using var scope = scopeFactory.CreateScope();
            using var tenant = scope.ServiceProvider.GetRequiredService<ITenantScope>().Enter(tenantId);
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.Webhooks.DeleteFinishedBeforeAsync(before, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

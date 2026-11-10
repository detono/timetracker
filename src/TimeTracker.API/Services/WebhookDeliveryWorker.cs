using Microsoft.Extensions.Options;
using TimeTracker.Infrastructure.Webhooks;

namespace TimeTracker.API.Services;

/// <summary>Sends queued webhook calls every few seconds, and clears out old history once an hour.</summary>
public class WebhookDeliveryWorker(
    WebhookDispatcher dispatcher,
    IOptions<WebhookOptions> options,
    ILogger<WebhookDeliveryWorker> logger
) : BackgroundService {
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CleanUpInterval = TimeSpan.FromHours(1);
    private const int MaxBatchesPerRound = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!options.Value.BackgroundSending) {
            return;
        }

        var nextCleanUp = DateTime.UtcNow;
        while (!stoppingToken.IsCancellationRequested) {
            try {
                // Work through a backlog a few batches at a time, then wait for the next round.
                for (var batch = 0; batch < MaxBatchesPerRound && await dispatcher.SendDueAsync(stoppingToken) > 0; batch++) { }

                if (DateTime.UtcNow >= nextCleanUp) {
                    await dispatcher.CleanUpAsync(stoppingToken);
                    nextCleanUp = DateTime.UtcNow + CleanUpInterval;
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception ex) {
                // E.g. the database is briefly unavailable: try again next round.
                logger.LogError(ex, "Sending webhooks failed.");
            }

            try {
                await Task.Delay(Interval, stoppingToken);
            } catch (OperationCanceledException) {
                return;
            }
        }
    }
}

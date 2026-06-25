using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Configuration;

namespace BillFlow.BackgroundJobs.Jobs;

public sealed class OverdueInvoiceSyncHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OverdueInvoiceSyncHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Clamp(
            BillFlowEnv.GetInt("OVERDUE_SYNC_INTERVAL_MINUTES", 60),
            5,
            1440);

        logger.LogInformation(
            "Overdue invoice sync started. Interval: {IntervalMinutes} minutes.",
            intervalMinutes);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        await RunSyncAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunSyncAsync(stoppingToken);
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
            var updated = await invoiceRepository.SyncOverdueStatusesForAllOwnersAsync(cancellationToken);

            if (updated > 0)
            {
                logger.LogInformation(
                    "Marked {UpdatedCount} invoice(s) as overdue.",
                    updated);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Overdue invoice sync failed.");
        }
    }
}

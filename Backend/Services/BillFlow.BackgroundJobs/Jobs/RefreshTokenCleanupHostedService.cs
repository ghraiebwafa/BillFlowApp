using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Configuration;

namespace BillFlow.BackgroundJobs.Jobs;

public sealed class RefreshTokenCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = Math.Clamp(
            BillFlowEnv.GetInt("REFRESH_TOKEN_CLEANUP_INTERVAL_HOURS", 24),
            1,
            168);

        logger.LogInformation(
            "Refresh token cleanup started. Interval: {IntervalHours} hours.",
            intervalHours);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));

        await RunCleanupAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCleanupAsync(stoppingToken);
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
            var deleted = await refreshTokenRepository.DeleteExpiredAsync(DateTime.UtcNow, cancellationToken);

            if (deleted > 0)
            {
                logger.LogInformation("Deleted {DeletedCount} expired refresh token(s).", deleted);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Refresh token cleanup failed.");
        }
    }
}

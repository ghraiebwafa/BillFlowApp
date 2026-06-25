namespace BillFlow.ManagementService.Services;

public sealed class SuperAdminSeederHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SuperAdminSeederHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<SuperAdminSeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "SuperAdmin seeding failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

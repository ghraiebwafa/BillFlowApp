namespace BillFlow.ManagementService.Services;

public sealed class SuperAdminSeederHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<SuperAdminSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

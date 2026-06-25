using BillFlow.BackgroundJobs;
using BillFlow.BackgroundJobs.Jobs;
using BillFlow.Database.Configuration;
using BillFlow.Database.DbContexts;
using BillFlow.Repositories;
using BillFlow.Shared.Configuration;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

if (string.Equals(
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
        "Development",
        StringComparison.OrdinalIgnoreCase)
    || string.Equals(
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        "Development",
        StringComparison.OrdinalIgnoreCase))
{
    Env.TraversePath().Load();
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<BillFlowDbContext>(options =>
    options.UseNpgsql(PostgresConnection.FromEnvironment()));

builder.Services.AddBillFlowRepositories();
builder.Services.AddHostedService<OverdueInvoiceSyncHostedService>();
builder.Services.AddHostedService<RefreshTokenCleanupHostedService>();

var host = builder.Build();
host.Run();

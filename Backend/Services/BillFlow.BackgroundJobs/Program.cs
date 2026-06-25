using BillFlow.BackgroundJobs;
using BillFlow.BackgroundJobs.Jobs;
using BillFlow.Database.Configuration;
using BillFlow.Database.DbContexts;
using BillFlow.Repositories;
using BillFlow.Shared.Configuration;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<BillFlowDbContext>(options =>
    options.UseNpgsql(PostgresConnection.FromEnvironment()));

builder.Services.AddBillFlowRepositories();
builder.Services.AddHostedService<OverdueInvoiceSyncHostedService>();

var host = builder.Build();
host.Run();

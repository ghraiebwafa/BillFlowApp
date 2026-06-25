extern alias Auth;

using BillFlow.Database.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace BillFlow.ManagementService.Tests;

public sealed class ManagementApiFixture : IAsyncLifetime
{
    private const string JwtSecret = "integration-test-jwt-secret-min-32-chars!";
    private const string RefreshPepper = "integration-test-refresh-pepper-value";
    private const string RedisPassword = "redis-integration-test-password";
    private const string DbName = "billflow_test";
    private const string DbUser = "billflow";
    private const string DbPassword = "billflow_test_password";
    private const string SuperAdminEmail = "superadmin@billflow.test";
    private const string SuperAdminPassword = "SuperAdminPass123!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase(DbName)
        .WithUsername(DbUser)
        .WithPassword(DbPassword)
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .WithCommand("redis-server", "--requirepass", RedisPassword)
        .Build();

    public WebApplicationFactory<Program> ManagementFactory { get; private set; } = null!;
    public WebApplicationFactory<Auth::Program> AuthFactory { get; private set; } = null!;

    public string SuperAdminEmailAddress => SuperAdminEmail;

    public string SuperAdminPasswordValue => SuperAdminPassword;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        ConfigureSharedEnvironment();
        await ApplyMigrationsAsync();

        ManagementFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });

        AuthFactory = new WebApplicationFactory<Auth::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("APPLY_MIGRATIONS", "false");
        });

        _ = ManagementFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await AuthFactory.DisposeAsync();
        await ManagementFactory.DisposeAsync();
        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private void ConfigureSharedEnvironment()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", JwtSecret);
        Environment.SetEnvironmentVariable("JWT_ISSUER", "BillFlow");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "BillFlow.Api");
        Environment.SetEnvironmentVariable("REFRESH_TOKEN_PEPPER", RefreshPepper);
        Environment.SetEnvironmentVariable("APPLY_MIGRATIONS", "false");
        Environment.SetEnvironmentVariable("ALLOW_DEV_RESET_PASSWORD", "false");
        Environment.SetEnvironmentVariable("DISABLE_RATE_LIMITING", "true");

        Environment.SetEnvironmentVariable("DB_HOST", _postgres.Hostname);
        Environment.SetEnvironmentVariable("DB_PORT", _postgres.GetMappedPublicPort(5432).ToString());
        Environment.SetEnvironmentVariable("DB_NAME", DbName);
        Environment.SetEnvironmentVariable("DB_USER", DbUser);
        Environment.SetEnvironmentVariable("DB_PASSWORD", DbPassword);

        Environment.SetEnvironmentVariable("REDIS_HOST", _redis.Hostname);
        Environment.SetEnvironmentVariable("REDIS_PORT", _redis.GetMappedPublicPort(6379).ToString());
        Environment.SetEnvironmentVariable("REDIS_PASSWORD", RedisPassword);

        Environment.SetEnvironmentVariable("SUPERADMIN_EMAIL", SuperAdminEmail);
        Environment.SetEnvironmentVariable("SUPERADMIN_PASSWORD", SuperAdminPassword);
        Environment.SetEnvironmentVariable("SUPERADMIN_FULL_NAME", "Integration Super Admin");
    }

    private async Task ApplyMigrationsAsync()
    {
        var options = new DbContextOptionsBuilder<BillFlowDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var db = new BillFlowDbContext(options);
        await db.Database.MigrateAsync();
    }
}

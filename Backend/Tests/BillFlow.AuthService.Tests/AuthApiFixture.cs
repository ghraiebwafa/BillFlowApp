using BillFlow.Database.DbContexts;
using BillFlow.Shared.Caching;
using BillFlow.Shared.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace BillFlow.AuthService.Tests;

public sealed class AuthApiFixture : IAsyncLifetime
{
    private const string JwtSecret = "integration-test-jwt-secret-min-32-chars!";
    private const string RefreshPepper = "integration-test-refresh-pepper-value";
    private const string RedisPassword = "redis-integration-test-password";
    private const string DbName = "billflow_test";
    private const string DbUser = "billflow";
    private const string DbPassword = "billflow_test_password";

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

    private string _dbConnectionString = null!;
    private string _redisConnectionString = null!;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        _dbConnectionString = _postgres.GetConnectionString();
        _redisConnectionString =
            $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)},password={RedisPassword},abortConnect=false";

        // Process-wide env is still set for code paths that read BillFlowEnv at host start,
        // but DbContext/Redis are pinned below so parallel Management tests cannot steal them.
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

        await ApplyMigrationsAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("APPLY_MIGRATIONS", "false");
            builder.ConfigureTestServices(PinInfrastructure);
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private void PinInfrastructure(IServiceCollection services)
    {
        services.RemoveAll<DbContextOptions<BillFlowDbContext>>();
        services.RemoveAll<BillFlowDbContext>();
        services.AddDbContext<BillFlowDbContext>(options =>
            options.UseNpgsql(_dbConnectionString));

        services.RemoveAll<RedisOptions>();
        services.RemoveAll<IConnectionMultiplexer>();
        services.RemoveAll<ICacheService>();

        var redisOptions = new RedisOptions
        {
            Host = _redis.Hostname,
            Port = _redis.GetMappedPublicPort(6379),
            Password = RedisPassword,
        };
        services.AddSingleton(redisOptions);
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(_redisConnectionString));
        services.AddSingleton<ICacheService, RedisCacheService>();
    }

    private async Task ApplyMigrationsAsync()
    {
        var options = new DbContextOptionsBuilder<BillFlowDbContext>()
            .UseNpgsql(_dbConnectionString)
            .Options;

        await using var db = new BillFlowDbContext(options);
        await db.Database.MigrateAsync();
    }
}

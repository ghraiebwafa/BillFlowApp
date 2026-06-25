using BillFlow.AuthService.Extensions;
using BillFlow.AuthService.Services;
using BillFlow.Database.Configuration;
using BillFlow.Database.DbContexts;
using BillFlow.Repositories;
using BillFlow.Shared.Configuration;
using BillFlow.Shared.Extensions;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BillFlow.AuthService;

public class Startup
{
    private readonly IHostEnvironment _environment;

    public Startup(IConfiguration configuration, IHostEnvironment environment)
    {
        _ = configuration;
        _environment = environment;
        Env.Load();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var jwtOptions = JwtOptions.FromEnvironment();
        var redisOptions = RedisOptions.FromEnvironment();

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddBillFlowRateLimiting();
        services.AddBillFlowCors(_environment);

        if (_environment.IsDevelopment())
            services.AddBillFlowSwagger();

        services.AddBillFlowRedis(redisOptions);
        services.AddBillFlowAuthTokens(jwtOptions, _environment);

        services.AddDbContext<BillFlowDbContext>(options =>
            options.UseNpgsql(PostgresConnection.FromEnvironment()));

        services.AddHttpContextAccessor();
        services.AddBillFlowRepositories();
        services.AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IAccountService, AccountService>();
    }

    public void Configure(WebApplication app)
    {
        if (ShouldApplyMigrations())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BillFlowDbContext>();
            db.Database.Migrate();
        }

        app.UseBillFlowSecurityHeaders();
        app.UseBillFlowHstsWhenProduction(app.Environment);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapGet("/health", async (
            BillFlowDbContext db,
            IConnectionMultiplexer redis,
            CancellationToken cancellationToken) =>
        {
            var postgresOk = await db.Database.CanConnectAsync(cancellationToken);

            bool redisOk;
            try
            {
                var latency = await redis.GetDatabase().PingAsync();
                redisOk = latency >= TimeSpan.Zero;
            }
            catch (RedisConnectionException)
            {
                redisOk = false;
            }

            var status = postgresOk && redisOk ? "healthy" : "degraded";
            var code = postgresOk && redisOk ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

            return Results.Json(
                new
                {
                    status,
                    postgres = postgresOk ? "up" : "down",
                    redis = redisOk ? "up" : "down",
                },
                statusCode: code);
        });

        app.UseBillFlowHttpsRedirection();
        app.UseCors(CorsExtensions.DefaultPolicyName);
        app.UseBillFlowRateLimitEmailParsing();
        if (!BillFlowEnv.GetBool("DISABLE_RATE_LIMITING", defaultValue: false))
            app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }

    private static bool ShouldApplyMigrations() =>
        string.Equals(
            Environment.GetEnvironmentVariable("APPLY_MIGRATIONS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}

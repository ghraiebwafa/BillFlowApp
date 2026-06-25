using BillFlow.Shared.Caching;
using BillFlow.Shared.Configuration;
using BillFlow.Shared.Security;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BillFlow.Shared.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection AddBillFlowRedis(
        this IServiceCollection services,
        RedisOptions redisOptions)
    {
        services.AddSingleton(redisOptions);
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisOptions.ConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = "BillFlow:";
        });

        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddScoped<ITokenSessionService, TokenSessionService>();
        return services;
    }
}

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace BillFlow.Shared.Caching;

public sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        if (bytes is null || bytes.Length == 0)
            return default;

        return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions();

        if (expiry.HasValue)
            options.SetAbsoluteExpiration(expiry.Value);

        return cache.SetAsync(key, bytes, options, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(key, cancellationToken);

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        return bytes is { Length: > 0 };
    }
}

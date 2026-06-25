namespace BillFlow.Shared.Caching;

public static class ResilientCacheExtensions
{
    public static async Task<bool> ExistsSafeAsync(
        this ICacheService cache,
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await cache.ExistsAsync(key, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    public static async Task SetSafeAsync<T>(
        this ICacheService cache,
        string key,
        T value,
        TimeSpan? expiry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.SetAsync(key, value, expiry, cancellationToken);
        }
        catch
        {
            // Redis is a cache; DB remains source of truth.
        }
    }
}

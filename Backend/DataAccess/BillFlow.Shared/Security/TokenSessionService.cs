using BillFlow.Shared.Caching;

namespace BillFlow.Shared.Security;

public sealed class TokenSessionService(ICacheService cache) : ITokenSessionService
{
    public async Task<int> GetTokenVersionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var version = await cache.GetAsync<int>(CacheKeys.TokenVersion(userId), cancellationToken);
        return version <= 0 ? 1 : version;
    }

    public async Task InvalidateAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var current = await GetTokenVersionAsync(userId, cancellationToken);
        await cache.SetAsync(CacheKeys.TokenVersion(userId), current + 1, expiry: null, cancellationToken);
    }
}

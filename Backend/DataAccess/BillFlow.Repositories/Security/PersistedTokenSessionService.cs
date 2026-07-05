using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Caching;
using BillFlow.Shared.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillFlow.Repositories.Security;

/// <summary>
/// Token versions are persisted on <see cref="User.TokenVersion"/> with Redis as a cache.
/// </summary>
public sealed class PersistedTokenSessionService(
    ICacheService cache,
    IUserRepository userRepository,
    ILogger<PersistedTokenSessionService> logger) : ITokenSessionService
{
    public async Task<int> GetTokenVersionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cached = await cache.GetAsync<int>(CacheKeys.TokenVersion(userId), cancellationToken);
            if (cached > 0)
                return cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable while reading token version for {UserId}", userId);
        }

        var version = await userRepository.GetTokenVersionAsync(userId, cancellationToken);

        try
        {
            await cache.SetAsync(CacheKeys.TokenVersion(userId), version, expiry: null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable while caching token version for {UserId}", userId);
        }

        return version;
    }

    public async Task InvalidateAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var next = await userRepository.IncrementTokenVersionAsync(userId, cancellationToken);

        try
        {
            await cache.RemoveAsync(CacheKeys.TokenVersion(userId), cancellationToken);
            await cache.SetAsync(CacheKeys.TokenVersion(userId), next, expiry: null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable while invalidating sessions for {UserId}", userId);
        }
    }
}

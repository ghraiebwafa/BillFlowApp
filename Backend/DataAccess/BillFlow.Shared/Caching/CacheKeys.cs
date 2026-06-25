namespace BillFlow.Shared.Caching;

/// <summary>Redis key prefixes for auth-related cache entries.</summary>
public static class CacheKeys
{
    public static string RevokedRefreshToken(string tokenHash) => $"auth:revoked-refresh:{tokenHash}";

    public static string TokenVersion(Guid userId) => $"auth:tv:{userId}";
}

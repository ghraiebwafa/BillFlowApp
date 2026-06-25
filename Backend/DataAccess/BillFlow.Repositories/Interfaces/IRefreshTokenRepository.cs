using BillFlow.Models.Entities;

namespace BillFlow.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(Guid refreshTokenId, string? replacedByTokenHash = null, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<RefreshTokenRotationResult?> RotateActiveTokenAsync(
        string tokenHash,
        RefreshToken replacement,
        CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}

using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.RefreshTokens;

public sealed class RefreshTokenRepository(BillFlowDbContext db) : IRefreshTokenRepository
{
    public async Task<RefreshToken> CreateAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        refreshToken.CreatedAt = DateTime.UtcNow;
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);
        return refreshToken;
    }

    public Task<RefreshToken?> GetActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(
                r => r.Token == tokenHash && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == tokenHash, cancellationToken);

    public async Task RevokeAsync(
        Guid refreshTokenId,
        string? replacedByTokenHash = null,
        CancellationToken cancellationToken = default)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Id == refreshTokenId, cancellationToken);
        if (token is null || token.RevokedAt is not null)
            return;

        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByToken = replacedByTokenHash;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }
}

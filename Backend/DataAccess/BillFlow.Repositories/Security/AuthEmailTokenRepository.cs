using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Security;

public sealed class AuthEmailTokenRepository(BillFlowDbContext db) : IAuthEmailTokenRepository
{
    public async Task CreateAsync(AuthEmailToken token, CancellationToken cancellationToken = default)
    {
        db.AuthEmailTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task InvalidateActiveAsync(
        Guid userId,
        AuthEmailTokenPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await db.AuthEmailTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.UsedAt, now),
                cancellationToken);
    }

    public Task<AuthEmailToken?> GetActiveByHashAsync(
        string tokenHash,
        AuthEmailTokenPurpose purpose,
        CancellationToken cancellationToken = default) =>
        db.AuthEmailTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash
                    && t.Purpose == purpose
                    && t.UsedAt == null
                    && t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

    public async Task MarkUsedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await db.AuthEmailTokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (token is null)
            return;

        token.UsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

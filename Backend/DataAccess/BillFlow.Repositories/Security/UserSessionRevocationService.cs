using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Security;

namespace BillFlow.Repositories.Security;

public sealed class UserSessionRevocationService(
    IRefreshTokenRepository refreshTokenRepository,
    ITokenSessionService tokenSession) : IUserSessionRevocationService
{
    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await refreshTokenRepository.RevokeAllForUserAsync(userId, cancellationToken);
        await tokenSession.InvalidateAllSessionsAsync(userId, cancellationToken);
    }
}

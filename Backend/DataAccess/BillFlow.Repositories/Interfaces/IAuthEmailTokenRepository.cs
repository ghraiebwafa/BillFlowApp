using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;

namespace BillFlow.Repositories.Interfaces;

public interface IAuthEmailTokenRepository
{
    Task CreateAsync(AuthEmailToken token, CancellationToken cancellationToken = default);

    Task InvalidateActiveAsync(Guid userId, AuthEmailTokenPurpose purpose, CancellationToken cancellationToken = default);

    Task<AuthEmailToken?> GetActiveByHashAsync(
        string tokenHash,
        AuthEmailTokenPurpose purpose,
        CancellationToken cancellationToken = default);

    Task MarkUsedAsync(Guid id, CancellationToken cancellationToken = default);
}

namespace BillFlow.Shared.Security;

public interface ITokenSessionService
{
    Task<int> GetTokenVersionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task InvalidateAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

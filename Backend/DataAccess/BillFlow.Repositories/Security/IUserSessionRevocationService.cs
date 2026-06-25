namespace BillFlow.Repositories.Security;

public interface IUserSessionRevocationService
{
    Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

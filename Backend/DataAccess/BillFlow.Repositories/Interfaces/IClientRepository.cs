using BillFlow.Models.Entities;

namespace BillFlow.Repositories.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid ownerId, Guid clientId, CancellationToken cancellationToken = default);

    Task<PagedResult<Client>> GetPagedAsync(
        Guid ownerId,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsForOwnerAsync(
        Guid ownerId,
        string email,
        Guid? excludeClientId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasInvoicesAsync(Guid ownerId, Guid clientId, CancellationToken cancellationToken = default);

    Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default);

    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid ownerId, Guid clientId, CancellationToken cancellationToken = default);
}

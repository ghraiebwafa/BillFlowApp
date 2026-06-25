using BillFlow.Models.Entities;

namespace BillFlow.Repositories.Interfaces;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid ownerId, Guid itemId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> GetAllAsync(
        Guid ownerId,
        string? search = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<bool> HasLineItemsAsync(
        Guid ownerId,
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<Item> CreateAsync(Item item, CancellationToken cancellationToken = default);

    Task UpdateAsync(Item item, CancellationToken cancellationToken = default);

    Task ArchiveAsync(Guid ownerId, Guid itemId, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid ownerId, Guid itemId, CancellationToken cancellationToken = default);
}

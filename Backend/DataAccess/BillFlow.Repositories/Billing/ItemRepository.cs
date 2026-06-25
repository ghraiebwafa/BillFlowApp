using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class ItemRepository(BillFlowDbContext db) : IItemRepository
{
    public Task<Item?> GetByIdAsync(
        Guid ownerId,
        Guid itemId,
        CancellationToken cancellationToken = default) =>
        db.Items.FirstOrDefaultAsync(
            i => i.OwnerId == ownerId && i.Id == itemId,
            cancellationToken);

    public async Task<IReadOnlyList<Item>> GetAllAsync(
        Guid ownerId,
        string? search = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Items.Where(i => i.OwnerId == ownerId);

        if (!includeArchived)
            query = query.Where(i => !i.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.Name, term)
                || (i.Description != null && EF.Functions.ILike(i.Description, term))
                || (i.Category != null && EF.Functions.ILike(i.Category, term)));
        }

        return await query
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasLineItemsAsync(
        Guid ownerId,
        Guid itemId,
        CancellationToken cancellationToken = default) =>
        db.InvoiceLineItems.AnyAsync(
            l => l.ItemId == itemId && l.Invoice.OwnerId == ownerId,
            cancellationToken);

    public async Task<Item> CreateAsync(Item item, CancellationToken cancellationToken = default)
    {
        item.CreatedAt = DateTime.UtcNow;

        db.Items.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task UpdateAsync(Item item, CancellationToken cancellationToken = default)
    {
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(
        Guid ownerId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var item = await db.Items
            .FirstOrDefaultAsync(i => i.OwnerId == ownerId && i.Id == itemId, cancellationToken);

        if (item is null)
            return;

        item.IsArchived = true;
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(
        Guid ownerId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var item = await db.Items
            .FirstOrDefaultAsync(i => i.OwnerId == ownerId && i.Id == itemId, cancellationToken);

        if (item is null)
            return;

        item.IsDeleted = true;
        item.IsActive = false;
        item.IsArchived = true;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

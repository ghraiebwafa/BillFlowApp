using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class ClientRepository(BillFlowDbContext db) : IClientRepository
{
    public Task<Client?> GetByIdAsync(
        Guid ownerId,
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        db.Clients.FirstOrDefaultAsync(
            c => c.OwnerId == ownerId && c.Id == clientId,
            cancellationToken);

    public async Task<IReadOnlyList<Client>> GetAllAsync(
        Guid ownerId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Clients.Where(c => c.OwnerId == ownerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.CompanyName, term)
                || EF.Functions.ILike(c.ContactName, term)
                || EF.Functions.ILike(c.Email, term));
        }

        return await query
            .OrderBy(c => c.CompanyName)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> EmailExistsForOwnerAsync(
        Guid ownerId,
        string email,
        Guid? excludeClientId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailNormalizer.Normalize(email);
        var query = db.Clients.Where(c => c.OwnerId == ownerId && c.Email == normalizedEmail);

        if (excludeClientId is not null)
            query = query.Where(c => c.Id != excludeClientId.Value);

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasInvoicesAsync(
        Guid ownerId,
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        db.Invoices.AnyAsync(
            i => i.OwnerId == ownerId && i.ClientId == clientId,
            cancellationToken);

    public async Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        client.Email = EmailNormalizer.Normalize(client.Email);
        client.CreatedAt = DateTime.UtcNow;

        db.Clients.Add(client);
        await db.SaveChangesAsync(cancellationToken);

        return client;
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        client.Email = EmailNormalizer.Normalize(client.Email);
        client.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(
        Guid ownerId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.OwnerId == ownerId && c.Id == clientId, cancellationToken);

        if (client is null)
            return;

        client.IsDeleted = true;
        client.IsActive = false;
        client.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class InvoiceRepository(BillFlowDbContext db) : IInvoiceRepository
{
    public Task<Invoice?> GetByIdAsync(
        Guid ownerId,
        Guid invoiceId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Invoices.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(i => i.Client)
                .Include(i => i.LineItems.OrderBy(l => l.SortOrder));
        }

        return query.FirstOrDefaultAsync(
            i => i.OwnerId == ownerId && i.Id == invoiceId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetAllAsync(
        Guid ownerId,
        InvoiceStatus? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Invoices
            .Include(i => i.Client)
            .Where(i => i.OwnerId == ownerId);

        if (status is not null)
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.InvoiceNumber, term)
                || EF.Functions.ILike(i.Client.CompanyName, term));
        }

        return await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> InvoiceNumberExistsAsync(
        Guid ownerId,
        string invoiceNumber,
        Guid? excludeInvoiceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Invoices.Where(i =>
            i.OwnerId == ownerId
            && i.InvoiceNumber == invoiceNumber);

        if (excludeInvoiceId is not null)
            query = query.Where(i => i.Id != excludeInvoiceId.Value);

        return query.AnyAsync(cancellationToken);
    }

    public Task<int> CountByOwnerAndYearAsync(
        Guid ownerId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);

        return db.Invoices.CountAsync(
            i => i.OwnerId == ownerId && i.CreatedAt >= start && i.CreatedAt < end,
            cancellationToken);
    }

    public async Task<Invoice> CreateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        invoice.CreatedAt = DateTime.UtcNow;

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        invoice.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteLineItemsAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        db.InvoiceLineItems
            .Where(l => l.InvoiceId == invoiceId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task SoftDeleteAsync(
        Guid ownerId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.OwnerId == ownerId && i.Id == invoiceId, cancellationToken);

        if (invoice is null)
            return;

        invoice.IsDeleted = true;
        invoice.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

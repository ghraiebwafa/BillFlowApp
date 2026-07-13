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
        var query = includeDetails
            ? db.Invoices.AsQueryable()
            : db.Invoices.AsNoTracking();

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

    public async Task<PagedResult<Invoice>> GetPagedAsync(
        Guid ownerId,
        InvoiceStatus? status = null,
        IReadOnlyCollection<InvoiceStatus>? statuses = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildListFilter(ownerId, status, statuses, search);

        var totalCount = await filter.CountAsync(cancellationToken);

        var items = await filter
            .Include(i => i.Client)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResult<Invoice>(items, totalCount);
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
            i => i.OwnerId == ownerId && i.InvoiceDate >= start && i.InvoiceDate < end,
            cancellationToken);
    }

    public async Task ReplaceLineItemsAsync(
        Invoice invoice,
        IReadOnlyList<InvoiceLineItem> lineItems,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.InvoiceLineItems
            .Where(l => l.InvoiceId == invoice.Id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var lineItem in lineItems)
        {
            lineItem.InvoiceId = invoice.Id;
            db.InvoiceLineItems.Add(lineItem);
        }

        invoice.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<int> SyncOverdueStatusesForAllOwnersAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return db.Invoices
            .Where(i =>
                i.Status == InvoiceStatus.Sent
                && i.DueDate < today)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(i => i.Status, InvoiceStatus.Overdue)
                    .SetProperty(i => i.UpdatedAt, DateTime.UtcNow),
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

    private IQueryable<Invoice> BuildListFilter(
        Guid ownerId,
        InvoiceStatus? status,
        IReadOnlyCollection<InvoiceStatus>? statuses,
        string? search)
    {
        var query = db.Invoices.AsNoTracking().Where(i => i.OwnerId == ownerId);

        if (statuses is { Count: > 0 })
            query = query.Where(i => statuses.Contains(i.Status));
        else if (status is not null)
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.InvoiceNumber, term)
                || EF.Functions.ILike(i.Client.CompanyName, term));
        }

        return query;
    }
}

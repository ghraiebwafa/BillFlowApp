using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class InvoiceShareTokenRepository(BillFlowDbContext db) : IInvoiceShareTokenRepository
{
    public Task<InvoiceShareToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return db.InvoiceShareTokens
            .Include(t => t.Invoice)
                .ThenInclude(i => i.Client)
            .Include(t => t.Invoice)
                .ThenInclude(i => i.LineItems.OrderBy(l => l.SortOrder))
            .FirstOrDefaultAsync(
                t => t.Token == token && !t.IsRevoked,
                cancellationToken);
    }

    public Task<InvoiceShareToken?> GetActiveByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        return db.InvoiceShareTokens
            .FirstOrDefaultAsync(
                t => t.InvoiceId == invoiceId
                     && !t.IsRevoked
                     && (t.ExpiresAt == null || t.ExpiresAt > DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<InvoiceShareToken> CreateAsync(
        InvoiceShareToken shareToken,
        CancellationToken cancellationToken = default)
    {
        shareToken.CreatedAt = DateTime.UtcNow;
        db.InvoiceShareTokens.Add(shareToken);
        await db.SaveChangesAsync(cancellationToken);
        return shareToken;
    }

    public Task RevokeByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return db.InvoiceShareTokens
            .Where(t => t.InvoiceId == invoiceId && !t.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.IsRevoked, true),
                cancellationToken);
    }
}

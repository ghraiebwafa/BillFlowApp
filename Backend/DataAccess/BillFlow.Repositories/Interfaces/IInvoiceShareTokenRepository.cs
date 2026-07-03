using BillFlow.Models.Entities;

namespace BillFlow.Repositories.Interfaces;

public interface IInvoiceShareTokenRepository
{
    Task<InvoiceShareToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<InvoiceShareToken?> GetActiveByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task<InvoiceShareToken> CreateAsync(InvoiceShareToken shareToken, CancellationToken cancellationToken = default);

    Task RevokeByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}

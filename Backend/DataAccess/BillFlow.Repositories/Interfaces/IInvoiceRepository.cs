using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;

namespace BillFlow.Repositories.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(
        Guid ownerId,
        Guid invoiceId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Invoice>> GetPagedAsync(
        Guid ownerId,
        InvoiceStatus? status = null,
        IReadOnlyCollection<InvoiceStatus>? statuses = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<bool> InvoiceNumberExistsAsync(
        Guid ownerId,
        string invoiceNumber,
        Guid? excludeInvoiceId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountByOwnerAndYearAsync(
        Guid ownerId,
        int year,
        CancellationToken cancellationToken = default);

    Task<Invoice> CreateAsync(Invoice invoice, CancellationToken cancellationToken = default);

    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);

    Task ReplaceLineItemsAsync(
        Invoice invoice,
        IReadOnlyList<InvoiceLineItem> lineItems,
        CancellationToken cancellationToken = default);

    Task<int> SyncOverdueStatusesForAllOwnersAsync(CancellationToken cancellationToken = default);

    Task DeleteLineItemsAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid ownerId, Guid invoiceId, CancellationToken cancellationToken = default);
}

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

    Task<IReadOnlyList<Invoice>> GetAllAsync(
        Guid ownerId,
        InvoiceStatus? status = null,
        string? search = null,
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

    Task DeleteLineItemsAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid ownerId, Guid invoiceId, CancellationToken cancellationToken = default);
}

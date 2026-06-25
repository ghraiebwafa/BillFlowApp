using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;

namespace BillFlow.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(
        Guid ownerId,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetByInvoiceAsync(
        Guid ownerId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetCompletedTotalForInvoiceAsync(
        Guid ownerId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<Payment> CreateAsync(Payment payment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);

    Task<Payment?> RecordPaymentWithInvoiceSyncAsync(
        Guid ownerId,
        Guid invoiceId,
        decimal amount,
        PaymentMethod method,
        DateTime paymentDate,
        string? reference,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<Payment?> ChangePaymentStatusWithInvoiceSyncAsync(
        Guid ownerId,
        Guid paymentId,
        PaymentStatus requiredStatus,
        PaymentStatus newStatus,
        CancellationToken cancellationToken = default);
}

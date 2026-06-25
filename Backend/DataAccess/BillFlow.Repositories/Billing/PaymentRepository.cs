using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class PaymentRepository(BillFlowDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(
        Guid ownerId,
        Guid paymentId,
        CancellationToken cancellationToken = default) =>
        db.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(
                p => p.OwnerId == ownerId && p.Id == paymentId,
                cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetByInvoiceAsync(
        Guid ownerId,
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        await db.Payments
            .Include(p => p.Invoice)
            .Where(p => p.OwnerId == ownerId && p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<decimal> GetCompletedTotalForInvoiceAsync(
        Guid ownerId,
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        db.Payments
            .Where(p =>
                p.OwnerId == ownerId
                && p.InvoiceId == invoiceId
                && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount, cancellationToken);

    public async Task<Payment> CreateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        payment.CreatedAt = DateTime.UtcNow;
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        payment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

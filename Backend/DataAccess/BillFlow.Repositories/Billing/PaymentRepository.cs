using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Billing;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

    public async Task<IReadOnlyList<Payment>> GetAllByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        await db.Payments
            .Include(p => p.Invoice)
            .Where(p => p.OwnerId == ownerId && p.Status == PaymentStatus.Completed)
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

    public async Task<Payment?> RecordPaymentWithInvoiceSyncAsync(
        Guid ownerId,
        Guid invoiceId,
        decimal amount,
        PaymentMethod method,
        DateTime paymentDate,
        string? reference,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var invoice = await db.Invoices
            .FirstOrDefaultAsync(
                i => i.OwnerId == ownerId && i.Id == invoiceId,
                cancellationToken);

        if (invoice is null)
            return null;

        var completedTotal = await db.Payments
            .Where(p =>
                p.OwnerId == ownerId
                && p.InvoiceId == invoiceId
                && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount, cancellationToken);

        if (completedTotal + amount > invoice.Total)
            return null;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            InvoiceId = invoiceId,
            Amount = amount,
            Method = method,
            Status = PaymentStatus.Completed,
            PaymentDate = paymentDate,
            Reference = reference?.Trim(),
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        db.Payments.Add(payment);

        invoice.Status = InvoicePaymentStatusCalculator.Resolve(
            invoice.Status,
            invoice.Total,
            completedTotal + amount,
            invoice.DueDate);
        invoice.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        payment.Invoice = invoice;
        return payment;
    }

    public async Task<Payment?> ChangePaymentStatusWithInvoiceSyncAsync(
        Guid ownerId,
        Guid paymentId,
        PaymentStatus requiredStatus,
        PaymentStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var payment = await db.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(
                p => p.OwnerId == ownerId && p.Id == paymentId,
                cancellationToken);

        if (payment is null || payment.Status != requiredStatus)
            return null;

        payment.Status = newStatus;
        payment.UpdatedAt = DateTime.UtcNow;

        var invoice = payment.Invoice;
        var completedTotal = await db.Payments
            .Where(p =>
                p.OwnerId == ownerId
                && p.InvoiceId == invoice.Id
                && p.Status == PaymentStatus.Completed
                && p.Id != paymentId)
            .SumAsync(p => p.Amount, cancellationToken);

        invoice.Status = InvoicePaymentStatusCalculator.Resolve(
            invoice.Status,
            invoice.Total,
            completedTotal,
            invoice.DueDate);
        invoice.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return payment;
    }
}

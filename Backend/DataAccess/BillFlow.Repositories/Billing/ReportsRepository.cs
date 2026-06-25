using BillFlow.Database.DbContexts;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class ReportsRepository(BillFlowDbContext db) : IReportsRepository
{
    public async Task<IReadOnlyList<SalesReportRow>> GetSalesAsync(
        Guid ownerId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Invoices
            .Include(i => i.Client)
            .Where(i => i.OwnerId == ownerId
                && (i.Status == InvoiceStatus.Sent
                    || i.Status == InvoiceStatus.Paid
                    || i.Status == InvoiceStatus.PartiallyPaid
                    || i.Status == InvoiceStatus.Overdue));

        query = ApplyInvoiceDateFilter(query, from, to);

        return await query
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new SalesReportRow
            {
                InvoiceNumber = i.InvoiceNumber,
                ClientCompanyName = i.Client.CompanyName,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                Status = i.Status,
                Subtotal = i.Subtotal,
                TaxAmount = i.TaxAmount,
                Total = i.Total,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentReportRow>> GetPaymentsAsync(
        Guid ownerId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Payments
            .Include(p => p.Invoice)
            .ThenInclude(i => i.Client)
            .Where(p => p.OwnerId == ownerId);

        if (from is not null)
            query = query.Where(p => p.PaymentDate >= ToUtcDate(from.Value));

        if (to is not null)
        {
            var toExclusive = ToUtcDate(to.Value).AddDays(1);
            query = query.Where(p => p.PaymentDate < toExclusive);
        }

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentReportRow
            {
                PaymentDate = p.PaymentDate,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                ClientCompanyName = p.Invoice.Client.CompanyName,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                Reference = p.Reference,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutstandingReportRow>> GetOutstandingAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var invoices = await db.Invoices
            .Include(i => i.Client)
            .Include(i => i.Payments)
            .Where(i =>
                i.OwnerId == ownerId
                && (i.Status == InvoiceStatus.Sent
                    || i.Status == InvoiceStatus.PartiallyPaid
                    || i.Status == InvoiceStatus.Overdue))
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

        return invoices
            .Select(i =>
            {
                var paid = i.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);

                return new OutstandingReportRow
                {
                    InvoiceNumber = i.InvoiceNumber,
                    ClientCompanyName = i.Client.CompanyName,
                    DueDate = i.DueDate,
                    Status = i.Status,
                    Total = i.Total,
                    Paid = paid,
                    Remaining = Math.Max(0m, i.Total - paid),
                };
            })
            .Where(r => r.Remaining > 0)
            .ToList();
    }

    public async Task<IReadOnlyList<TaxReportRow>> GetTaxesAsync(
        Guid ownerId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Invoices
            .Include(i => i.Client)
            .Where(i => i.OwnerId == ownerId && i.TaxAmount > 0);

        query = ApplyInvoiceDateFilter(query, from, to);

        return await query
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new TaxReportRow
            {
                InvoiceNumber = i.InvoiceNumber,
                ClientCompanyName = i.Client.CompanyName,
                InvoiceDate = i.InvoiceDate,
                Subtotal = i.Subtotal,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                Total = i.Total,
            })
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Models.Entities.Invoice> ApplyInvoiceDateFilter(
        IQueryable<Models.Entities.Invoice> query,
        DateTime? from,
        DateTime? to)
    {
        if (from is not null)
            query = query.Where(i => i.InvoiceDate >= ToUtcDate(from.Value));

        if (to is not null)
        {
            var toExclusive = ToUtcDate(to.Value).AddDays(1);
            query = query.Where(i => i.InvoiceDate < toExclusive);
        }

        return query;
    }

    private static DateTime ToUtcDate(DateTime date) =>
        DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
}

using BillFlow.Database.DbContexts;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class ReportsRepository(BillFlowDbContext db) : IReportsRepository
{
    public const int MaxReportRows = 5_000;

    public async Task<IReadOnlyList<SalesReportRow>> GetSalesAsync(
        Guid ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var query = db.Invoices
            .AsNoTracking()
            .Where(i => i.OwnerId == ownerId
                && (i.Status == InvoiceStatus.Sent
                    || i.Status == InvoiceStatus.Paid
                    || i.Status == InvoiceStatus.PartiallyPaid
                    || i.Status == InvoiceStatus.Overdue));

        query = ApplyInvoiceDateFilter(query, from, to);

        return await query
            .OrderByDescending(i => i.InvoiceDate)
            .Take(MaxReportRows)
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
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = ToUtcDate(from);
        var toExclusive = ToUtcDate(to).AddDays(1);

        return await db.Payments
            .AsNoTracking()
            .Where(p =>
                p.OwnerId == ownerId
                && p.PaymentDate >= fromUtc
                && p.PaymentDate < toExclusive)
            .OrderByDescending(p => p.PaymentDate)
            .Take(MaxReportRows)
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
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = ToUtcDate(from);
        var toExclusive = ToUtcDate(to).AddDays(1);

        return await db.Invoices
            .AsNoTracking()
            .Where(i =>
                i.OwnerId == ownerId
                && (i.Status == InvoiceStatus.Sent
                    || i.Status == InvoiceStatus.PartiallyPaid
                    || i.Status == InvoiceStatus.Overdue)
                && i.DueDate >= fromUtc
                && i.DueDate < toExclusive)
            .OrderBy(i => i.DueDate)
            .Select(i => new
            {
                i.InvoiceNumber,
                ClientCompanyName = i.Client.CompanyName,
                i.DueDate,
                i.Status,
                i.Total,
                Paid = i.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount),
            })
            .Where(x => x.Total - x.Paid > 0)
            .Take(MaxReportRows)
            .Select(x => new OutstandingReportRow
            {
                InvoiceNumber = x.InvoiceNumber,
                ClientCompanyName = x.ClientCompanyName,
                DueDate = x.DueDate,
                Status = x.Status,
                Total = x.Total,
                Paid = x.Paid,
                Remaining = x.Total - x.Paid,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxReportRow>> GetTaxesAsync(
        Guid ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var query = db.Invoices
            .AsNoTracking()
            .Where(i => i.OwnerId == ownerId && i.TaxAmount > 0);

        query = ApplyInvoiceDateFilter(query, from, to);

        return await query
            .OrderByDescending(i => i.InvoiceDate)
            .Take(MaxReportRows)
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
        DateTime from,
        DateTime to)
    {
        var fromUtc = ToUtcDate(from);
        var toExclusive = ToUtcDate(to).AddDays(1);
        return query.Where(i => i.InvoiceDate >= fromUtc && i.InvoiceDate < toExclusive);
    }

    private static DateTime ToUtcDate(DateTime date) =>
        DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
}

using BillFlow.Database.DbContexts;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class DashboardRepository(BillFlowDbContext db) : IDashboardRepository
{
    public async Task<DashboardResponse> GetDashboardAsync(
        Guid ownerId,
        int revenueMonths = 12,
        int topClientsLimit = 5,
        CancellationToken cancellationToken = default)
    {
        revenueMonths = Math.Clamp(revenueMonths, 1, 24);
        topClientsLimit = Math.Clamp(topClientsLimit, 1, 20);

        var utcNow = DateTime.UtcNow;
        var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var revenueStart = monthStart.AddMonths(-(revenueMonths - 1));
        var today = utcNow.Date;

        var invoiceStats = await db.Invoices
            .AsNoTracking()
            .Where(i => i.OwnerId == ownerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalInvoices = g.Count(),
                OverdueInvoicesCount = g.Count(i =>
                    i.Status == InvoiceStatus.Overdue
                    || ((i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.PartiallyPaid)
                        && i.DueDate < today)),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var paymentStats = await db.Payments
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId && p.Status == PaymentStatus.Completed)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalRevenue = g.Sum(p => p.Amount),
                MonthlyIncome = g
                    .Where(p => p.PaymentDate >= monthStart)
                    .Sum(p => p.Amount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var activeClientsCount = await db.Clients
            .AsNoTracking()
            .CountAsync(c => c.OwnerId == ownerId && c.IsActive, cancellationToken);

        var pendingPaymentsAmount = await db.Invoices
            .AsNoTracking()
            .Where(i =>
                i.OwnerId == ownerId
                && (i.Status == InvoiceStatus.Sent
                    || i.Status == InvoiceStatus.PartiallyPaid
                    || i.Status == InvoiceStatus.Overdue))
            .Select(i => new
            {
                Remaining = i.Total - i.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount),
            })
            .Where(x => x.Remaining > 0)
            .SumAsync(x => x.Remaining, cancellationToken);

        var revenueByMonth = await db.Payments
            .AsNoTracking()
            .Where(p =>
                p.OwnerId == ownerId
                && p.Status == PaymentStatus.Completed
                && p.PaymentDate >= revenueStart)
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new DashboardMonthlyRevenuePoint
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(p => p.Amount),
            })
            .OrderBy(p => p.Year)
            .ThenBy(p => p.Month)
            .ToListAsync(cancellationToken);

        var invoicesByStatus = await db.Invoices
            .AsNoTracking()
            .Where(i => i.OwnerId == ownerId)
            .GroupBy(i => i.Status)
            .Select(g => new DashboardStatusCount
            {
                Status = g.Key,
                Count = g.Count(),
            })
            .OrderBy(s => s.Status)
            .ToListAsync(cancellationToken);

        var paymentsByMethod = await db.Payments
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId && p.Status == PaymentStatus.Completed)
            .GroupBy(p => p.Method)
            .Select(g => new DashboardPaymentMethodSummary
            {
                Method = g.Key,
                Amount = g.Sum(p => p.Amount),
            })
            .OrderByDescending(p => p.Amount)
            .ToListAsync(cancellationToken);

        var topClients = await db.Payments
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId && p.Status == PaymentStatus.Completed)
            .GroupBy(p => new { p.Invoice.ClientId, p.Invoice.Client.CompanyName })
            .Select(g => new DashboardTopClient
            {
                ClientId = g.Key.ClientId,
                CompanyName = g.Key.CompanyName,
                Revenue = g.Sum(p => p.Amount),
            })
            .OrderByDescending(c => c.Revenue)
            .Take(topClientsLimit)
            .ToListAsync(cancellationToken);

        return new DashboardResponse
        {
            TotalRevenue = paymentStats?.TotalRevenue ?? 0m,
            TotalInvoices = invoiceStats?.TotalInvoices ?? 0,
            PendingPaymentsAmount = pendingPaymentsAmount,
            OverdueInvoicesCount = invoiceStats?.OverdueInvoicesCount ?? 0,
            ActiveClientsCount = activeClientsCount,
            MonthlyIncome = paymentStats?.MonthlyIncome ?? 0m,
            RevenueByMonth = revenueByMonth,
            InvoicesByStatus = invoicesByStatus,
            PaymentsByMethod = paymentsByMethod,
            TopClients = topClients,
        };
    }
}

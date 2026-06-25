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

        var totalRevenue = await db.Payments
            .Where(p => p.OwnerId == ownerId && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount, cancellationToken);

        var monthlyIncome = await db.Payments
            .Where(p =>
                p.OwnerId == ownerId
                && p.Status == PaymentStatus.Completed
                && p.PaymentDate >= monthStart)
            .SumAsync(p => p.Amount, cancellationToken);

        var totalInvoices = await db.Invoices
            .CountAsync(i => i.OwnerId == ownerId, cancellationToken);

        var activeClientsCount = await db.Clients
            .CountAsync(c => c.OwnerId == ownerId && c.IsActive, cancellationToken);

        var openInvoices = await db.Invoices
            .Where(i =>
                i.OwnerId == ownerId
                && (i.Status == InvoiceStatus.Sent
                    || i.Status == InvoiceStatus.PartiallyPaid
                    || i.Status == InvoiceStatus.Overdue))
            .Select(i => new
            {
                i.Total,
                Paid = i.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount),
            })
            .ToListAsync(cancellationToken);

        var pendingPaymentsAmount = openInvoices
            .Sum(i => Math.Max(0m, i.Total - i.Paid));

        var overdueInvoicesCount = await db.Invoices
            .CountAsync(
                i => i.OwnerId == ownerId
                    && (i.Status == InvoiceStatus.Overdue
                        || ((i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.PartiallyPaid)
                            && i.DueDate < today)),
                cancellationToken);

        var revenueByMonth = await db.Payments
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
            TotalRevenue = totalRevenue,
            TotalInvoices = totalInvoices,
            PendingPaymentsAmount = pendingPaymentsAmount,
            OverdueInvoicesCount = overdueInvoicesCount,
            ActiveClientsCount = activeClientsCount,
            MonthlyIncome = monthlyIncome,
            RevenueByMonth = revenueByMonth,
            InvoicesByStatus = invoicesByStatus,
            PaymentsByMethod = paymentsByMethod,
            TopClients = topClients,
        };
    }
}

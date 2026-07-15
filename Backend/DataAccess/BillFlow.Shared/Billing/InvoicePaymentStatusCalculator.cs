using BillFlow.Models.Shared.Enums;

namespace BillFlow.Shared.Billing;

public static class InvoicePaymentStatusCalculator
{
    public static InvoiceStatus Resolve(
        InvoiceStatus currentStatus,
        decimal invoiceTotal,
        decimal completedTotal,
        DateTime? dueDateUtc = null)
    {
        if (currentStatus is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            return currentStatus;

        if (completedTotal >= invoiceTotal)
            return InvoiceStatus.Paid;

        // Past-due open balances are Overdue whether unpaid or partially paid —
        // aligns with dashboard KPI and SyncOverdueStatusesForAllOwnersAsync.
        if (IsPastDue(dueDateUtc))
            return InvoiceStatus.Overdue;

        if (completedTotal > 0)
            return InvoiceStatus.PartiallyPaid;

        return currentStatus switch
        {
            InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid or InvoiceStatus.Overdue => InvoiceStatus.Sent,
            _ => currentStatus,
        };
    }

    private static bool IsPastDue(DateTime? dueDateUtc) =>
        dueDateUtc.HasValue && dueDateUtc.Value.Date < DateTime.UtcNow.Date;
}

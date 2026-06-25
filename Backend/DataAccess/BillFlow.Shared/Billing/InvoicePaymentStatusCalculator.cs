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

        if (completedTotal <= 0)
        {
            var reopened = currentStatus switch
            {
                InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid => InvoiceStatus.Sent,
                _ => currentStatus,
            };

            return IsPastDue(dueDateUtc) && reopened is InvoiceStatus.Sent or InvoiceStatus.Overdue
                ? InvoiceStatus.Overdue
                : reopened;
        }

        if (completedTotal >= invoiceTotal)
            return InvoiceStatus.Paid;

        return InvoiceStatus.PartiallyPaid;
    }

    private static bool IsPastDue(DateTime? dueDateUtc) =>
        dueDateUtc.HasValue && dueDateUtc.Value.Date < DateTime.UtcNow.Date;
}

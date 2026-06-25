using BillFlow.Models.Shared.Enums;

namespace BillFlow.Shared.Billing;

public static class InvoicePaymentStatusCalculator
{
    public static InvoiceStatus Resolve(InvoiceStatus currentStatus, decimal invoiceTotal, decimal completedTotal)
    {
        if (currentStatus is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            return currentStatus;

        if (completedTotal <= 0)
        {
            return currentStatus switch
            {
                InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid => InvoiceStatus.Sent,
                _ => currentStatus,
            };
        }

        if (completedTotal >= invoiceTotal)
            return InvoiceStatus.Paid;

        return InvoiceStatus.PartiallyPaid;
    }
}

using BillFlow.Models.Shared.Enums;

namespace BillFlow.ManagementService.Services.Billing;

public static class InvoiceStatusRules
{
    public static bool CanEdit(InvoiceStatus status) => status == InvoiceStatus.Draft;

    public static bool CanDelete(InvoiceStatus status) => status == InvoiceStatus.Draft;

    public static bool CanSend(InvoiceStatus status) => status == InvoiceStatus.Draft;

    public static bool CanMarkPaid(InvoiceStatus status) =>
        status is InvoiceStatus.Sent or InvoiceStatus.Overdue or InvoiceStatus.PartiallyPaid;

    public static bool CanReceivePayment(InvoiceStatus status) =>
        status is InvoiceStatus.Sent or InvoiceStatus.Overdue or InvoiceStatus.PartiallyPaid;

    public static bool CanCancel(InvoiceStatus status) =>
        status is InvoiceStatus.Draft or InvoiceStatus.Sent;
}

using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class PaymentReportRow
{
    public DateTime PaymentDate { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public string ClientCompanyName { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    public string? Reference { get; set; }
}

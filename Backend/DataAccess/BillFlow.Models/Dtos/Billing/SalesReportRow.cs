using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class SalesReportRow
{
    public string InvoiceNumber { get; set; } = null!;

    public string ClientCompanyName { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public DateTime DueDate { get; set; }

    public InvoiceStatus Status { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }
}

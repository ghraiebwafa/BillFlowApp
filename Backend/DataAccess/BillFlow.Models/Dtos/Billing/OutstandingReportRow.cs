using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class OutstandingReportRow
{
    public string InvoiceNumber { get; set; } = null!;

    public string ClientCompanyName { get; set; } = null!;

    public DateTime DueDate { get; set; }

    public InvoiceStatus Status { get; set; }

    public decimal Total { get; set; }

    public decimal Paid { get; set; }

    public decimal Remaining { get; set; }
}

using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class InvoiceSummaryResponse
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public InvoiceStatus Status { get; set; }

    public Guid ClientId { get; set; }

    public string ClientCompanyName { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public DateTime DueDate { get; set; }

    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }
}

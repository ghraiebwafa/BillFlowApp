using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class InvoiceDetailResponse
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public InvoiceStatus Status { get; set; }

    public Guid ClientId { get; set; }

    public string ClientCompanyName { get; set; } = null!;

    public string ClientContactName { get; set; } = null!;

    public string ClientEmail { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public DateTime DueDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<InvoiceLineItemResponse> LineItems { get; set; } = [];
}

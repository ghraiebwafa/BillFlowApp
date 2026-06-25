using System.ComponentModel.DataAnnotations;
using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class CreateInvoiceRequest
{
    [Required]
    public Guid ClientId { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    [Range(0, 100)]
    public decimal TaxRate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<InvoiceLineItemRequest> LineItems { get; set; } = [];
}

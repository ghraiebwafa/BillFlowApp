using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Billing;

public class InvoiceLineItemRequest
{
    public Guid? ItemId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = null!;

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

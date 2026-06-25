using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Billing;

public class UpdateItemRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Range(0, 100)]
    public decimal VatRate { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    public bool IsActive { get; set; } = true;
}

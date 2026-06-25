namespace BillFlow.Models.Dtos.Billing;

public class ItemResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }

    public string Currency { get; set; } = null!;

    public decimal VatRate { get; set; }

    public string? Category { get; set; }

    public string? Unit { get; set; }

    public bool IsActive { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

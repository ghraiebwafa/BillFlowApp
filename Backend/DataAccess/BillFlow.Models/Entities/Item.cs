namespace BillFlow.Models.Entities;

public class Item
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }

    public string Currency { get; set; } = "USD";

    public decimal VatRate { get; set; }

    public string? Category { get; set; }

    public string? Unit { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsArchived { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}

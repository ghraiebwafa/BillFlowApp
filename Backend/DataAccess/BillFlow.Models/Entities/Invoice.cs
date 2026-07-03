using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Entities;

public class Invoice
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateTime InvoiceDate { get; set; }

    public DateTime DueDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<InvoiceShareToken> ShareTokens { get; set; } = new List<InvoiceShareToken>();
}

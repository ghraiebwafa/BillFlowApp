using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public Guid InvoiceId { get; set; }

    public Invoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;

    public DateTime PaymentDate { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

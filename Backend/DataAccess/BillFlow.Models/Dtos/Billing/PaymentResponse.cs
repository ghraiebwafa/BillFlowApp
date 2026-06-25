using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class PaymentResponse
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

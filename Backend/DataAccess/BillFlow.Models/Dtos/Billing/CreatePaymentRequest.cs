using System.ComponentModel.DataAnnotations;
using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class CreatePaymentRequest
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod Method { get; set; }

    public DateTime? PaymentDate { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

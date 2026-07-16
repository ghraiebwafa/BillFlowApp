using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Billing;

public class UpsertCompanySettingsRequest
{
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = null!;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(50)]
    public string? TaxNumber { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    [MaxLength(20)]
    public string InvoiceNumberPrefix { get; set; } = "INV";

    [Range(0, 100)]
    public decimal DefaultTaxRate { get; set; }

    [Range(1, 365)]
    public int PaymentTermsDays { get; set; } = 30;

    [MaxLength(100)]
    public string? TimeZone { get; set; }

    [RegularExpression(@"^#?[0-9A-Fa-f]{6}$")]
    [MaxLength(7)]
    public string? BrandColor { get; set; }

    [MaxLength(500)]
    public string? InvoiceFooterNote { get; set; }

    public bool EnablePaymentReminders { get; set; }

    [Range(0, 30)]
    public int ReminderDaysBeforeDue { get; set; } = 3;
}

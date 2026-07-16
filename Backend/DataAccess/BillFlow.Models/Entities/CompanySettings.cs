namespace BillFlow.Models.Entities;

public class CompanySettings
{
    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string? Address { get; set; }

    public string? Country { get; set; }

    public string? TaxNumber { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string Currency { get; set; } = "USD";

    public string InvoiceNumberPrefix { get; set; } = "INV";

    public decimal DefaultTaxRate { get; set; }

    public int PaymentTermsDays { get; set; } = 30;

    public string? TimeZone { get; set; }

    public string? BrandColor { get; set; }

    public string? InvoiceFooterNote { get; set; }

    public byte[]? LogoBytes { get; set; }

    public string? LogoContentType { get; set; }

    public bool EnablePaymentReminders { get; set; }

    public int ReminderDaysBeforeDue { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

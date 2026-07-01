namespace BillFlow.Models.Dtos.Billing;

public class CompanySettingsResponse
{
    public string CompanyName { get; set; } = null!;

    public string? Address { get; set; }

    public string? Country { get; set; }

    public string? TaxNumber { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string Currency { get; set; } = null!;

    public string InvoiceNumberPrefix { get; set; } = null!;

    public decimal DefaultTaxRate { get; set; }

    public int PaymentTermsDays { get; set; }

    public string? TimeZone { get; set; }

    public string? BrandColor { get; set; }

    public string? InvoiceFooterNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

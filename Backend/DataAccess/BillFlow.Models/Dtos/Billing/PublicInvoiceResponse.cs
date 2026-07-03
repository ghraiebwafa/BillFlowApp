using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class PublicInvoiceResponse
{
    public string InvoiceNumber { get; set; } = null!;

    public InvoiceStatus Status { get; set; }

    public string ClientCompanyName { get; set; } = null!;

    public string ClientContactName { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public DateTime DueDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public string? Notes { get; set; }

    public IReadOnlyList<InvoiceLineItemResponse> LineItems { get; set; } = [];

    public PublicIssuerInfo? Issuer { get; set; }
}

public class PublicIssuerInfo
{
    public string CompanyName { get; set; } = null!;

    public string? Address { get; set; }

    public string? Country { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? TaxNumber { get; set; }

    public string Currency { get; set; } = "USD";

    public string? BrandColor { get; set; }

    public string? InvoiceFooterNote { get; set; }
}

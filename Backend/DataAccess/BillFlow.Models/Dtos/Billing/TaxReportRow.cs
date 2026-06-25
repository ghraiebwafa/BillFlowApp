namespace BillFlow.Models.Dtos.Billing;

public class TaxReportRow
{
    public string InvoiceNumber { get; set; } = null!;

    public string ClientCompanyName { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }
}

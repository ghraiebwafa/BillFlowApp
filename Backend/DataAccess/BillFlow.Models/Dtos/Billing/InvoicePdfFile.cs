namespace BillFlow.Models.Dtos.Billing;

public class InvoicePdfFile
{
    public byte[] Content { get; set; } = [];

    public string FileName { get; set; } = null!;
}

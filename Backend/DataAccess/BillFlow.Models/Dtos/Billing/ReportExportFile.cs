namespace BillFlow.Models.Dtos.Billing;

public class ReportExportFile
{
    public byte[] Content { get; set; } = [];

    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;
}

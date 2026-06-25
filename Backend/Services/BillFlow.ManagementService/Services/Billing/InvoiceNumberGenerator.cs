namespace BillFlow.ManagementService.Services.Billing;

public static class InvoiceNumberGenerator
{
    public static string Generate(string prefix, int year, int sequenceNumber) =>
        $"{prefix}-{year}-{sequenceNumber:D4}";
}

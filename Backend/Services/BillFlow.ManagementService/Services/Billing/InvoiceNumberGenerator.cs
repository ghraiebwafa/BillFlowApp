namespace BillFlow.ManagementService.Services.Billing;

public static class InvoiceNumberGenerator
{
    public static string Generate(int year, int sequenceNumber) =>
        $"INV-{year}-{sequenceNumber:D4}";
}

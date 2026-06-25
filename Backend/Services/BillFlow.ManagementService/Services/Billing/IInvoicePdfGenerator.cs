using BillFlow.Models.Dtos.Billing;

namespace BillFlow.ManagementService.Services.Billing;

public interface IInvoicePdfGenerator
{
    byte[] Generate(InvoiceDetailResponse invoice);
}

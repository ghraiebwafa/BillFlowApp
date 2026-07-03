using BillFlow.Models.Dtos.Billing;

namespace BillFlow.ManagementService.Services;

public interface IPortalService
{
    Task<OperationResult<PublicInvoiceResponse>> GetInvoiceByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InvoicePdfFile>> DownloadPdfByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}

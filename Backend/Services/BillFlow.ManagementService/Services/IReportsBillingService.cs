using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;

namespace BillFlow.ManagementService.Services;

public interface IReportsBillingService
{
    Task<OperationResult<ReportExportFile>> ExportSalesAsync(
        ReportFormat format,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ReportExportFile>> ExportPaymentsAsync(
        ReportFormat format,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ReportExportFile>> ExportOutstandingAsync(
        ReportFormat format,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ReportExportFile>> ExportTaxesAsync(
        ReportFormat format,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}

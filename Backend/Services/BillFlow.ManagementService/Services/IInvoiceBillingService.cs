using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;

namespace BillFlow.ManagementService.Services;

public interface IInvoiceBillingService
{
    Task<OperationResult<PagedResponse<InvoiceSummaryResponse>>> GetAllAsync(
        InvoiceStatus? status = null,
        IReadOnlyList<InvoiceStatus>? statuses = null,
        string? search = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InvoiceDetailResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InvoiceDetailResponse>> CreateAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InvoiceDetailResponse>> UpdateAsync(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InvoiceDetailResponse>> DuplicateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<MessageResponse>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InvoiceDetailResponse>> SendAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<MessageResponse>> EmailInvoiceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InvoiceDetailResponse>> MarkPaidAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InvoiceDetailResponse>> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InvoicePdfFile>> DownloadPdfAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ShareLinkResponse>> GenerateShareLinkAsync(
        Guid id,
        string portalBaseUrl,
        CancellationToken cancellationToken = default);

    Task<OperationResult<MessageResponse>> RevokeShareLinkAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

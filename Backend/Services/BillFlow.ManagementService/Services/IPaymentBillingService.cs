using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;

namespace BillFlow.ManagementService.Services;

public interface IPaymentBillingService
{
    Task<OperationResult<IReadOnlyList<PaymentResponse>>> GetByInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<PaymentResponse>> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<PaymentResponse>> RefundAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<PaymentResponse>> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

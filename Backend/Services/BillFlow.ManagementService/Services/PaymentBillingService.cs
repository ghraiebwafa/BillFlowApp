using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class PaymentBillingService(
    IPaymentRepository paymentRepository,
    IInvoiceRepository invoiceRepository,
    IAuditTrailService auditTrail,
    ICurrentUserAccessor currentUser) : IPaymentBillingService
{
    private const int MaxFuturePaymentDays = 1;

    public async Task<OperationResult<IReadOnlyList<PaymentResponse>>> GetByInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<IReadOnlyList<PaymentResponse>>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;

        var invoice = await invoiceRepository.GetByIdAsync(owner, invoiceId, cancellationToken: cancellationToken);
        if (invoice is null)
        {
            return OperationResult<IReadOnlyList<PaymentResponse>>.Fail(
                "Invoice not found.",
                StatusCodes.Status404NotFound);
        }

        var payments = await paymentRepository.GetByInvoiceAsync(owner, invoiceId, cancellationToken);
        return OperationResult<IReadOnlyList<PaymentResponse>>.Ok(payments.Select(Map).ToList());
    }

    public async Task<OperationResult<PagedResponse<PaymentResponse>>> GetAllAsync(
        string? search = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<PagedResponse<PaymentResponse>>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;
        var (normalizedPage, normalizedPageSize) = BillingPaging.Normalize(page, pageSize);
        var result = await paymentRepository.GetPagedByOwnerAsync(
            owner,
            search,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);

        return OperationResult<PagedResponse<PaymentResponse>>.Ok(
            PagedResponse<PaymentResponse>.Create(
                result.Items.Select(Map).ToList(),
                result.TotalCount,
                normalizedPage,
                normalizedPageSize));
    }

    public async Task<OperationResult<PaymentResponse>> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<PaymentResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;

        var invoice = await invoiceRepository.GetByIdAsync(owner, request.InvoiceId, cancellationToken: cancellationToken);
        if (invoice is null)
        {
            return OperationResult<PaymentResponse>.Fail(
                "Invoice not found.",
                StatusCodes.Status404NotFound);
        }

        if (!InvoiceStatusRules.CanReceivePayment(invoice.Status))
        {
            return OperationResult<PaymentResponse>.Fail(
                "Payments can only be recorded for sent, overdue, or partially paid invoices.",
                StatusCodes.Status400BadRequest);
        }

        if (!TryValidatePaymentDate(request.PaymentDate, out var dateError))
        {
            return OperationResult<PaymentResponse>.Fail(
                dateError!,
                StatusCodes.Status400BadRequest);
        }

        var paymentDate = ToUtcDate(request.PaymentDate ?? DateTime.UtcNow);

        var payment = await paymentRepository.RecordPaymentWithInvoiceSyncAsync(
            owner,
            invoice.Id,
            request.Amount,
            request.Method,
            paymentDate,
            request.Reference,
            request.Notes,
            cancellationToken);

        if (payment is null)
        {
            return OperationResult<PaymentResponse>.Fail(
                "Payment amount exceeds the remaining invoice balance.",
                StatusCodes.Status400BadRequest);
        }

        await auditTrail.LogAsync(
            owner,
            AuditAction.PaymentRecorded,
            AuditEntityType.Payment,
            payment.Id,
            $"Payment of {payment.Amount:C} recorded for invoice {payment.Invoice.InvoiceNumber}.",
            cancellationToken);

        return OperationResult<PaymentResponse>.Ok(Map(payment), StatusCodes.Status201Created);
    }

    public async Task<OperationResult<PaymentResponse>> RefundAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<PaymentResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        return await ChangePaymentStatusAsync(
            ownerId.Value!.Value,
            id,
            PaymentStatus.Completed,
            PaymentStatus.Refunded,
            AuditAction.Refunded,
            "Only completed payments can be refunded.",
            cancellationToken);
    }

    public async Task<OperationResult<PaymentResponse>> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<PaymentResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        return await ChangePaymentStatusAsync(
            ownerId.Value!.Value,
            id,
            PaymentStatus.Completed,
            PaymentStatus.Cancelled,
            AuditAction.Cancelled,
            "Only completed payments can be cancelled.",
            cancellationToken);
    }

    private async Task<OperationResult<PaymentResponse>> ChangePaymentStatusAsync(
        Guid ownerId,
        Guid paymentId,
        PaymentStatus requiredStatus,
        PaymentStatus newStatus,
        AuditAction auditAction,
        string invalidMessage,
        CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.ChangePaymentStatusWithInvoiceSyncAsync(
            ownerId,
            paymentId,
            requiredStatus,
            newStatus,
            cancellationToken);

        if (payment is null)
        {
            var existing = await paymentRepository.GetByIdAsync(ownerId, paymentId, cancellationToken);
            if (existing is null)
            {
                return OperationResult<PaymentResponse>.Fail(
                    "Payment not found.",
                    StatusCodes.Status404NotFound);
            }

            return OperationResult<PaymentResponse>.Fail(
                invalidMessage,
                StatusCodes.Status400BadRequest);
        }

        await auditTrail.LogAsync(
            ownerId,
            auditAction,
            AuditEntityType.Payment,
            payment.Id,
            $"Payment for invoice {payment.Invoice.InvoiceNumber} {auditAction.ToString().ToLowerInvariant()}.",
            cancellationToken);

        return OperationResult<PaymentResponse>.Ok(Map(payment));
    }

    private static bool TryValidatePaymentDate(DateTime? paymentDate, out string? error)
    {
        if (paymentDate is null)
        {
            error = null;
            return true;
        }

        var maxAllowed = DateTime.UtcNow.Date.AddDays(MaxFuturePaymentDays);
        if (paymentDate.Value.Date > maxAllowed)
        {
            error = $"Payment date cannot be more than {MaxFuturePaymentDays} day(s) in the future.";
            return false;
        }

        error = null;
        return true;
    }

    private static DateTime ToUtcDate(DateTime date) =>
        DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

    private static PaymentResponse Map(Payment payment) => new()
    {
        Id = payment.Id,
        InvoiceId = payment.InvoiceId,
        InvoiceNumber = payment.Invoice.InvoiceNumber,
        Amount = payment.Amount,
        Method = payment.Method,
        Status = payment.Status,
        PaymentDate = payment.PaymentDate,
        Reference = payment.Reference,
        Notes = payment.Notes,
        CreatedAt = payment.CreatedAt,
        UpdatedAt = payment.UpdatedAt,
    };
}

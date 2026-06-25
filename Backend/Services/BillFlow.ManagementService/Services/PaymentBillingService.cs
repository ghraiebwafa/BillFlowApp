using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Constants;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class PaymentBillingService(
    IPaymentRepository paymentRepository,
    IInvoiceRepository invoiceRepository,
    ICurrentUserAccessor currentUser) : IPaymentBillingService
{
    public async Task<OperationResult<IReadOnlyList<PaymentResponse>>> GetByInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<IReadOnlyList<PaymentResponse>>();
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

    public async Task<OperationResult<PaymentResponse>> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<PaymentResponse>();
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

        var completedTotal = await paymentRepository.GetCompletedTotalForInvoiceAsync(
            owner,
            invoice.Id,
            cancellationToken);

        if (completedTotal + request.Amount > invoice.Total)
        {
            return OperationResult<PaymentResponse>.Fail(
                "Payment amount exceeds the remaining invoice balance.",
                StatusCodes.Status400BadRequest);
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OwnerId = owner,
            InvoiceId = invoice.Id,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Completed,
            PaymentDate = ToUtcDate(request.PaymentDate ?? DateTime.UtcNow),
            Reference = request.Reference?.Trim(),
            Notes = request.Notes?.Trim(),
        };

        await paymentRepository.CreateAsync(payment, cancellationToken);
        await SyncInvoicePaymentStatusAsync(owner, invoice, cancellationToken);

        payment.Invoice = invoice;
        return OperationResult<PaymentResponse>.Ok(Map(payment), StatusCodes.Status201Created);
    }

    public async Task<OperationResult<PaymentResponse>> RefundAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<PaymentResponse>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        return await ChangePaymentStatusAsync(
            ownerId.Value!.Value,
            id,
            PaymentStatus.Completed,
            PaymentStatus.Refunded,
            "Only completed payments can be refunded.",
            cancellationToken);
    }

    public async Task<OperationResult<PaymentResponse>> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<PaymentResponse>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        return await ChangePaymentStatusAsync(
            ownerId.Value!.Value,
            id,
            PaymentStatus.Completed,
            PaymentStatus.Cancelled,
            "Only completed payments can be cancelled.",
            cancellationToken);
    }

    private async Task<OperationResult<PaymentResponse>> ChangePaymentStatusAsync(
        Guid ownerId,
        Guid paymentId,
        PaymentStatus requiredStatus,
        PaymentStatus newStatus,
        string invalidMessage,
        CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByIdAsync(ownerId, paymentId, cancellationToken);
        if (payment is null)
        {
            return OperationResult<PaymentResponse>.Fail(
                "Payment not found.",
                StatusCodes.Status404NotFound);
        }

        if (payment.Status != requiredStatus)
        {
            return OperationResult<PaymentResponse>.Fail(
                invalidMessage,
                StatusCodes.Status400BadRequest);
        }

        payment.Status = newStatus;
        await paymentRepository.UpdateAsync(payment, cancellationToken);
        await SyncInvoicePaymentStatusAsync(ownerId, payment.Invoice, cancellationToken);

        var updated = await paymentRepository.GetByIdAsync(ownerId, paymentId, cancellationToken);
        return OperationResult<PaymentResponse>.Ok(Map(updated!));
    }

    private async Task SyncInvoicePaymentStatusAsync(
        Guid ownerId,
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var completedTotal = await paymentRepository.GetCompletedTotalForInvoiceAsync(
            ownerId,
            invoice.Id,
            cancellationToken);

        invoice.Status = InvoicePaymentStatusCalculator.Resolve(
            invoice.Status,
            invoice.Total,
            completedTotal);

        await invoiceRepository.UpdateAsync(invoice, cancellationToken);
    }

    private static DateTime ToUtcDate(DateTime date) =>
        DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

    private (Guid? Value, OperationResult<T>? Error) RequireBusinessOwnerId<T>()
    {
        if (!IsBusinessOwner())
        {
            return (null, OperationResult<T>.Fail(
                "Business owner role is required.",
                StatusCodes.Status403Forbidden));
        }

        if (currentUser.UserId is null)
        {
            return (null, OperationResult<T>.Fail(
                "Authentication required.",
                StatusCodes.Status401Unauthorized));
        }

        return (currentUser.UserId, null);
    }

    private bool IsBusinessOwner() =>
        string.Equals(currentUser.Role, RoleNames.Visitor, StringComparison.OrdinalIgnoreCase);

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

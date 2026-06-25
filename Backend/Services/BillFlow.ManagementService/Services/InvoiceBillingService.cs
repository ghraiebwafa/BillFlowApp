using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.ManagementService.Services.Billing;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.ManagementService.Services;

public sealed class InvoiceBillingService(
    IInvoiceRepository invoiceRepository,
    IClientRepository clientRepository,
    IItemRepository itemRepository,
    IPaymentRepository paymentRepository,
    ICompanySettingsRepository companySettingsRepository,
    IInvoicePdfGenerator invoicePdfGenerator,
    ICurrentUserAccessor currentUser) : IInvoiceBillingService
{
    private const int MaxInvoiceNumberRetries = 5;
    private const string DefaultInvoicePrefix = "INV";
    private const int DefaultPaymentTermsDays = 30;

    public async Task<OperationResult<IReadOnlyList<InvoiceSummaryResponse>>> GetAllAsync(
        InvoiceStatus? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<IReadOnlyList<InvoiceSummaryResponse>>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;
        await invoiceRepository.SyncOverdueStatusesAsync(owner, cancellationToken);

        var invoices = await invoiceRepository.GetAllAsync(owner, status, search, cancellationToken);

        return OperationResult<IReadOnlyList<InvoiceSummaryResponse>>.Ok(
            invoices.Select(MapSummary).ToList());
    }

    public async Task<OperationResult<InvoiceDetailResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<InvoiceDetailResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;
        await invoiceRepository.SyncOverdueStatusesAsync(owner, cancellationToken);

        var invoice = await invoiceRepository.GetByIdAsync(
            owner,
            id,
            includeDetails: true,
            cancellationToken);

        if (invoice is null)
            return NotFound<InvoiceDetailResponse>();

        return OperationResult<InvoiceDetailResponse>.Ok(MapDetail(invoice));
    }

    public async Task<OperationResult<InvoiceDetailResponse>> CreateAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<InvoiceDetailResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;

        var clientError = await ValidateClientAsync(owner, request.ClientId, cancellationToken);
        if (clientError is not null)
            return clientError;

        var lineItemError = await ValidateLineItemsAsync(owner, request.LineItems, cancellationToken);
        if (lineItemError is not null)
            return lineItemError;

        var companySettings = await companySettingsRepository.GetByOwnerAsync(owner, cancellationToken);
        var taxRate = request.TaxRate;
        if (taxRate == 0 && companySettings is not null)
            taxRate = companySettings.DefaultTaxRate;

        var invoiceDate = ToUtcDate(request.InvoiceDate ?? DateTime.UtcNow);
        var paymentTermsDays = companySettings?.PaymentTermsDays ?? DefaultPaymentTermsDays;
        var dueDate = ToUtcDate(request.DueDate ?? invoiceDate.AddDays(paymentTermsDays));
        var invoicePrefix = companySettings?.InvoiceNumberPrefix ?? DefaultInvoicePrefix;

        if (!TryValidateDates(invoiceDate, dueDate, out var dateError))
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                dateError!,
                StatusCodes.Status400BadRequest);
        }

        var (lineItems, subtotal, taxAmount, total) =
            InvoiceCalculator.BuildLineItems(request.LineItems, taxRate);

        for (var attempt = 0; attempt < MaxInvoiceNumberRetries; attempt++)
        {
            var invoiceNumber = await GenerateInvoiceNumberAsync(
                owner,
                invoicePrefix,
                invoiceDate.Year,
                cancellationToken);

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OwnerId = owner,
                ClientId = request.ClientId,
                InvoiceNumber = invoiceNumber,
                Status = InvoiceStatus.Draft,
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                Subtotal = subtotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = total,
                Notes = request.Notes?.Trim(),
                LineItems = lineItems.ToList(),
            };

            foreach (var lineItem in invoice.LineItems)
                lineItem.InvoiceId = invoice.Id;

            try
            {
                await invoiceRepository.CreateAsync(invoice, cancellationToken);

                var created = await invoiceRepository.GetByIdAsync(
                    owner,
                    invoice.Id,
                    includeDetails: true,
                    cancellationToken);

                return OperationResult<InvoiceDetailResponse>.Ok(
                    MapDetail(created!),
                    StatusCodes.Status201Created);
            }
            catch (DbUpdateException) when (attempt < MaxInvoiceNumberRetries - 1)
            {
            }
        }

        return OperationResult<InvoiceDetailResponse>.Fail(
            "Unable to generate a unique invoice number. Please try again.",
            StatusCodes.Status409Conflict);
    }

    public async Task<OperationResult<InvoiceDetailResponse>> UpdateAsync(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<InvoiceDetailResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;

        var invoice = await invoiceRepository.GetByIdAsync(
            owner,
            id,
            includeDetails: true,
            cancellationToken);

        if (invoice is null)
            return NotFound<InvoiceDetailResponse>();

        if (!InvoiceStatusRules.CanEdit(invoice.Status))
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                "Only draft invoices can be updated.",
                StatusCodes.Status400BadRequest);
        }

        var clientError = await ValidateClientAsync(owner, request.ClientId, cancellationToken);
        if (clientError is not null)
            return clientError;

        var lineItemError = await ValidateLineItemsAsync(owner, request.LineItems, cancellationToken);
        if (lineItemError is not null)
            return lineItemError;

        var invoiceDate = ToUtcDate(request.InvoiceDate);
        var dueDate = ToUtcDate(request.DueDate);

        if (!TryValidateDates(invoiceDate, dueDate, out var dateError))
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                dateError!,
                StatusCodes.Status400BadRequest);
        }

        var (lineItems, subtotal, taxAmount, total) =
            InvoiceCalculator.BuildLineItems(request.LineItems, request.TaxRate);

        invoice.ClientId = request.ClientId;
        invoice.InvoiceDate = invoiceDate;
        invoice.DueDate = dueDate;
        invoice.Subtotal = subtotal;
        invoice.TaxRate = request.TaxRate;
        invoice.TaxAmount = taxAmount;
        invoice.Total = total;
        invoice.Notes = request.Notes?.Trim();

        await invoiceRepository.ReplaceLineItemsAsync(invoice, lineItems.ToList(), cancellationToken);

        var updated = await invoiceRepository.GetByIdAsync(
            owner,
            invoice.Id,
            includeDetails: true,
            cancellationToken);

        return OperationResult<InvoiceDetailResponse>.Ok(MapDetail(updated!));
    }

    public async Task<OperationResult<InvoiceDetailResponse>> DuplicateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<InvoiceDetailResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;

        var source = await invoiceRepository.GetByIdAsync(
            owner,
            id,
            includeDetails: true,
            cancellationToken);

        if (source is null)
            return NotFound<InvoiceDetailResponse>();

        var request = new CreateInvoiceRequest
        {
            ClientId = source.ClientId,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            TaxRate = source.TaxRate,
            Notes = source.Notes,
            LineItems = source.LineItems
                .OrderBy(l => l.SortOrder)
                .Select(l => new InvoiceLineItemRequest
                {
                    ItemId = l.ItemId,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                })
                .ToList(),
        };

        return await CreateAsync(request, cancellationToken);
    }

    public async Task<OperationResult<MessageResponse>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<MessageResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;

        var invoice = await invoiceRepository.GetByIdAsync(owner, id, cancellationToken: cancellationToken);
        if (invoice is null)
            return NotFound<MessageResponse>();

        if (!InvoiceStatusRules.CanDelete(invoice.Status))
        {
            return OperationResult<MessageResponse>.Fail(
                "Only draft invoices can be deleted.",
                StatusCodes.Status400BadRequest);
        }

        await invoiceRepository.SoftDeleteAsync(owner, id, cancellationToken);
        return OperationResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Invoice deleted successfully.",
        });
    }

    public async Task<OperationResult<InvoiceDetailResponse>> SendAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<InvoiceDetailResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        return await ChangeStatusAsync(
            ownerId.Value!.Value,
            id,
            InvoiceStatusRules.CanSend,
            InvoiceStatus.Sent,
            "Only draft invoices can be sent.",
            cancellationToken);
    }

    public async Task<OperationResult<InvoiceDetailResponse>> MarkPaidAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<InvoiceDetailResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;
        await invoiceRepository.SyncOverdueStatusesAsync(owner, cancellationToken);

        var invoice = await invoiceRepository.GetByIdAsync(
            owner,
            id,
            includeDetails: true,
            cancellationToken);

        if (invoice is null)
            return NotFound<InvoiceDetailResponse>();

        if (!InvoiceStatusRules.CanMarkPaid(invoice.Status))
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                "Only sent, overdue, or partially paid invoices can be marked as paid.",
                StatusCodes.Status400BadRequest);
        }

        var completedTotal = await paymentRepository.GetCompletedTotalForInvoiceAsync(
            owner,
            invoice.Id,
            cancellationToken);

        if (completedTotal < invoice.Total)
        {
            var payment = await paymentRepository.RecordPaymentWithInvoiceSyncAsync(
                owner,
                invoice.Id,
                invoice.Total - completedTotal,
                PaymentMethod.Cash,
                DateTime.UtcNow,
                "Mark paid adjustment",
                null,
                cancellationToken);

            if (payment is null)
            {
                return OperationResult<InvoiceDetailResponse>.Fail(
                    "Unable to record the remaining payment for this invoice.",
                    StatusCodes.Status409Conflict);
            }
        }
        else if (invoice.Status != InvoiceStatus.Paid)
        {
            invoice.Status = InvoiceStatus.Paid;
            await invoiceRepository.UpdateAsync(invoice, cancellationToken);
        }

        var updated = await invoiceRepository.GetByIdAsync(
            owner,
            id,
            includeDetails: true,
            cancellationToken);

        return OperationResult<InvoiceDetailResponse>.Ok(MapDetail(updated!));
    }

    public async Task<OperationResult<InvoiceDetailResponse>> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<InvoiceDetailResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        return await ChangeStatusAsync(
            ownerId.Value!.Value,
            id,
            InvoiceStatusRules.CanCancel,
            InvoiceStatus.Cancelled,
            "Only draft or sent invoices can be cancelled.",
            cancellationToken);
    }

    public async Task<OperationResult<InvoicePdfFile>> DownloadPdfAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<InvoicePdfFile>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;
        await invoiceRepository.SyncOverdueStatusesAsync(owner, cancellationToken);

        var invoice = await invoiceRepository.GetByIdAsync(
            owner,
            id,
            includeDetails: true,
            cancellationToken);

        if (invoice is null)
        {
            return OperationResult<InvoicePdfFile>.Fail(
                "Invoice not found.",
                StatusCodes.Status404NotFound);
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return OperationResult<InvoicePdfFile>.Fail(
                "Cancelled invoices cannot be downloaded as PDF.",
                StatusCodes.Status400BadRequest);
        }

        if (invoice.Status == InvoiceStatus.Draft)
        {
            return OperationResult<InvoicePdfFile>.Fail(
                "Draft invoices cannot be downloaded as PDF. Send the invoice first.",
                StatusCodes.Status400BadRequest);
        }

        var detail = MapDetail(invoice);
        var companySettings = await companySettingsRepository.GetByOwnerAsync(owner, cancellationToken);
        var issuer = companySettings is null
            ? null
            : CompanySettingsBillingService.Map(companySettings);
        var content = invoicePdfGenerator.Generate(detail, issuer);

        return OperationResult<InvoicePdfFile>.Ok(new InvoicePdfFile
        {
            Content = content,
            FileName = $"{SanitizeFileName(detail.InvoiceNumber)}.pdf",
        });
    }

    private async Task<OperationResult<InvoiceDetailResponse>> ChangeStatusAsync(
        Guid ownerId,
        Guid invoiceId,
        Func<InvoiceStatus, bool> canTransition,
        InvoiceStatus newStatus,
        string invalidMessage,
        CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(
            ownerId,
            invoiceId,
            includeDetails: true,
            cancellationToken);

        if (invoice is null)
            return NotFound<InvoiceDetailResponse>();

        if (!canTransition(invoice.Status))
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                invalidMessage,
                StatusCodes.Status400BadRequest);
        }

        invoice.Status = newStatus;
        await invoiceRepository.UpdateAsync(invoice, cancellationToken);

        return OperationResult<InvoiceDetailResponse>.Ok(MapDetail(invoice));
    }

    private async Task<OperationResult<InvoiceDetailResponse>?> ValidateClientAsync(
        Guid ownerId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(ownerId, clientId, cancellationToken);
        if (client is null)
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                "Client not found.",
                StatusCodes.Status404NotFound);
        }

        if (!client.IsActive)
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                "Cannot create or update invoices for an inactive client.",
                StatusCodes.Status400BadRequest);
        }

        return null;
    }

    private async Task<string> GenerateInvoiceNumberAsync(
        Guid ownerId,
        string prefix,
        int year,
        CancellationToken cancellationToken)
    {
        var sequence = await invoiceRepository.CountByOwnerAndYearAsync(ownerId, year, cancellationToken) + 1;
        var invoiceNumber = InvoiceNumberGenerator.Generate(prefix, year, sequence);

        while (await invoiceRepository.InvoiceNumberExistsAsync(ownerId, invoiceNumber, cancellationToken: cancellationToken))
        {
            sequence++;
            invoiceNumber = InvoiceNumberGenerator.Generate(prefix, year, sequence);
        }

        return invoiceNumber;
    }

    private async Task<OperationResult<InvoiceDetailResponse>?> ValidateLineItemsAsync(
        Guid ownerId,
        IReadOnlyList<InvoiceLineItemRequest> lineItems,
        CancellationToken cancellationToken)
    {
        foreach (var lineItem in lineItems)
        {
            if (lineItem.ItemId is null)
                continue;

            var item = await itemRepository.GetByIdAsync(ownerId, lineItem.ItemId.Value, cancellationToken);
            if (item is null)
            {
                return OperationResult<InvoiceDetailResponse>.Fail(
                    "One or more catalog items were not found.",
                    StatusCodes.Status404NotFound);
            }

            if (!item.IsActive || item.IsArchived)
            {
                return OperationResult<InvoiceDetailResponse>.Fail(
                    $"Item '{item.Name}' is archived or inactive and cannot be used on invoices.",
                    StatusCodes.Status400BadRequest);
            }
        }

        return null;
    }

    private static bool TryValidateDates(DateTime invoiceDate, DateTime dueDate, out string? error)
    {
        if (dueDate.Date < invoiceDate.Date)
        {
            error = "Due date cannot be before invoice date.";
            return false;
        }

        error = null;
        return true;
    }

    private static DateTime ToUtcDate(DateTime date) =>
        DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

    private static OperationResult<T> NotFound<T>() =>
        OperationResult<T>.Fail("Invoice not found.", StatusCodes.Status404NotFound);

    private static string SanitizeFileName(string invoiceNumber)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(invoiceNumber.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }

    private static InvoiceSummaryResponse MapSummary(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        Status = invoice.Status,
        ClientId = invoice.ClientId,
        ClientCompanyName = invoice.Client.CompanyName,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        Total = invoice.Total,
        CreatedAt = invoice.CreatedAt,
    };

    private static InvoiceDetailResponse MapDetail(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        Status = invoice.Status,
        ClientId = invoice.ClientId,
        ClientCompanyName = invoice.Client.CompanyName,
        ClientContactName = invoice.Client.ContactName,
        ClientEmail = invoice.Client.Email,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        Subtotal = invoice.Subtotal,
        TaxRate = invoice.TaxRate,
        TaxAmount = invoice.TaxAmount,
        Total = invoice.Total,
        Notes = invoice.Notes,
        CreatedAt = invoice.CreatedAt,
        UpdatedAt = invoice.UpdatedAt,
        LineItems = invoice.LineItems
            .OrderBy(l => l.SortOrder)
            .Select(l => new InvoiceLineItemResponse
            {
                Id = l.Id,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                SortOrder = l.SortOrder,
            })
            .ToList(),
    };
}

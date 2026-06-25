using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Constants;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class InvoiceBillingService(
    IInvoiceRepository invoiceRepository,
    IClientRepository clientRepository,
    IItemRepository itemRepository,
    ICurrentUserAccessor currentUser) : IInvoiceBillingService
{
    public async Task<OperationResult<IReadOnlyList<InvoiceSummaryResponse>>> GetAllAsync(
        InvoiceStatus? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<IReadOnlyList<InvoiceSummaryResponse>>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        var invoices = await invoiceRepository.GetAllAsync(
            ownerId.Value!.Value,
            status,
            search,
            cancellationToken);

        return OperationResult<IReadOnlyList<InvoiceSummaryResponse>>.Ok(
            invoices.Select(MapSummary).ToList());
    }

    public async Task<OperationResult<InvoiceDetailResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<InvoiceDetailResponse>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        var invoice = await invoiceRepository.GetByIdAsync(
            ownerId.Value!.Value,
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
        var ownerId = RequireBusinessOwnerId<InvoiceDetailResponse>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;

        var client = await clientRepository.GetByIdAsync(owner, request.ClientId, cancellationToken);
        if (client is null)
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                "Client not found.",
                StatusCodes.Status404NotFound);
        }

        var lineItemError = await ValidateLineItemsAsync(owner, request.LineItems, cancellationToken);
        if (lineItemError is not null)
            return lineItemError;

        var invoiceDate = ToUtcDate(request.InvoiceDate ?? DateTime.UtcNow);
        var dueDate = ToUtcDate(request.DueDate ?? invoiceDate.AddDays(30));

        if (!TryValidateDates(invoiceDate, dueDate, out var dateError))
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                dateError!,
                StatusCodes.Status400BadRequest);
        }

        var (lineItems, subtotal, taxAmount, total) =
            InvoiceCalculator.BuildLineItems(request.LineItems, request.TaxRate);

        var invoiceNumber = await GenerateInvoiceNumberAsync(owner, invoiceDate.Year, cancellationToken);

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
            TaxRate = request.TaxRate,
            TaxAmount = taxAmount,
            Total = total,
            Notes = request.Notes?.Trim(),
            LineItems = lineItems.ToList(),
        };

        foreach (var lineItem in invoice.LineItems)
            lineItem.InvoiceId = invoice.Id;

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

    public async Task<OperationResult<InvoiceDetailResponse>> UpdateAsync(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<InvoiceDetailResponse>();
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

        var client = await clientRepository.GetByIdAsync(owner, request.ClientId, cancellationToken);
        if (client is null)
        {
            return OperationResult<InvoiceDetailResponse>.Fail(
                "Client not found.",
                StatusCodes.Status404NotFound);
        }

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

        invoice.LineItems.Clear();
        await invoiceRepository.DeleteLineItemsAsync(invoice.Id, cancellationToken);

        invoice.ClientId = request.ClientId;
        invoice.InvoiceDate = invoiceDate;
        invoice.DueDate = dueDate;
        invoice.Subtotal = subtotal;
        invoice.TaxRate = request.TaxRate;
        invoice.TaxAmount = taxAmount;
        invoice.Total = total;
        invoice.Notes = request.Notes?.Trim();

        foreach (var lineItem in lineItems)
        {
            lineItem.InvoiceId = invoice.Id;
            invoice.LineItems.Add(lineItem);
        }

        await invoiceRepository.UpdateAsync(invoice, cancellationToken);

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
        var ownerId = RequireBusinessOwnerId<InvoiceDetailResponse>();
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
        var ownerId = RequireBusinessOwnerId<MessageResponse>();
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
        var ownerId = RequireBusinessOwnerId<InvoiceDetailResponse>();
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
        var ownerId = RequireBusinessOwnerId<InvoiceDetailResponse>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        return await ChangeStatusAsync(
            ownerId.Value!.Value,
            id,
            InvoiceStatusRules.CanMarkPaid,
            InvoiceStatus.Paid,
            "Only sent or overdue invoices can be marked as paid.",
            cancellationToken);
    }

    public async Task<OperationResult<InvoiceDetailResponse>> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<InvoiceDetailResponse>();
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

    private async Task<string> GenerateInvoiceNumberAsync(
        Guid ownerId,
        int year,
        CancellationToken cancellationToken)
    {
        var sequence = await invoiceRepository.CountByOwnerAndYearAsync(ownerId, year, cancellationToken) + 1;
        var invoiceNumber = InvoiceNumberGenerator.Generate(year, sequence);

        while (await invoiceRepository.InvoiceNumberExistsAsync(ownerId, invoiceNumber, cancellationToken: cancellationToken))
        {
            sequence++;
            invoiceNumber = InvoiceNumberGenerator.Generate(year, sequence);
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
                    $"Item '{lineItem.ItemId}' was not found.",
                    StatusCodes.Status404NotFound);
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

    private static OperationResult<T> NotFound<T>() =>
        OperationResult<T>.Fail("Invoice not found.", StatusCodes.Status404NotFound);

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

using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class ItemBillingService(
    IItemRepository itemRepository,
    IAuditTrailService auditTrail,
    ICurrentUserAccessor currentUser) : IItemBillingService
{
    public async Task<OperationResult<PagedResponse<ItemResponse>>> GetAllAsync(
        string? search = null,
        bool includeArchived = false,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<PagedResponse<ItemResponse>>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var (normalizedPage, normalizedPageSize) = BillingPaging.Normalize(page, pageSize);
        var result = await itemRepository.GetPagedAsync(
            ownerId.Value!.Value,
            search,
            includeArchived,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);

        return OperationResult<PagedResponse<ItemResponse>>.Ok(
            PagedResponse<ItemResponse>.Create(
                result.Items.Select(Map).ToList(),
                result.TotalCount,
                normalizedPage,
                normalizedPageSize));
    }

    public async Task<OperationResult<ItemResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<ItemResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var item = await itemRepository.GetByIdAsync(ownerId.Value!.Value, id, cancellationToken);
        if (item is null)
            return NotFound<ItemResponse>();

        return OperationResult<ItemResponse>.Ok(Map(item));
    }

    public async Task<OperationResult<ItemResponse>> CreateAsync(
        CreateItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<ItemResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return OperationResult<ItemResponse>.Fail(
                "Item name is required.",
                StatusCodes.Status400BadRequest);
        }

        var item = new Item
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId.Value!.Value,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            UnitPrice = request.UnitPrice,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            VatRate = request.VatRate,
            Category = request.Category?.Trim(),
            Unit = request.Unit?.Trim(),
            IsActive = true,
        };

        await itemRepository.CreateAsync(item, cancellationToken);
        await auditTrail.LogAsync(
            ownerId.Value!.Value,
            AuditAction.Created,
            AuditEntityType.Item,
            item.Id,
            $"Item \"{item.Name}\" created.",
            cancellationToken);
        return OperationResult<ItemResponse>.Ok(Map(item), StatusCodes.Status201Created);
    }

    public async Task<OperationResult<ItemResponse>> UpdateAsync(
        Guid id,
        UpdateItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<ItemResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var item = await itemRepository.GetByIdAsync(ownerId.Value!.Value, id, cancellationToken);
        if (item is null)
            return NotFound<ItemResponse>();

        if (item.IsArchived)
        {
            return OperationResult<ItemResponse>.Fail(
                "Archived items cannot be updated. Create a new item instead.",
                StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return OperationResult<ItemResponse>.Fail(
                "Item name is required.",
                StatusCodes.Status400BadRequest);
        }

        item.Name = request.Name.Trim();
        item.Description = request.Description?.Trim();
        item.UnitPrice = request.UnitPrice;
        item.Currency = request.Currency.Trim().ToUpperInvariant();
        item.VatRate = request.VatRate;
        item.Category = request.Category?.Trim();
        item.Unit = request.Unit?.Trim();
        item.IsActive = request.IsActive;

        await itemRepository.UpdateAsync(item, cancellationToken);
        await auditTrail.LogAsync(
            ownerId.Value!.Value,
            AuditAction.Updated,
            AuditEntityType.Item,
            item.Id,
            $"Item \"{item.Name}\" updated.",
            cancellationToken);
        return OperationResult<ItemResponse>.Ok(Map(item));
    }

    public async Task<OperationResult<MessageResponse>> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<MessageResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var item = await itemRepository.GetByIdAsync(ownerId.Value!.Value, id, cancellationToken);
        if (item is null)
            return NotFound<MessageResponse>();

        if (item.IsArchived)
        {
            return OperationResult<MessageResponse>.Ok(new MessageResponse
            {
                Message = "Item is already archived.",
            });
        }

        await itemRepository.ArchiveAsync(ownerId.Value.Value, id, cancellationToken);
        await auditTrail.LogAsync(
            ownerId.Value.Value,
            AuditAction.Archived,
            AuditEntityType.Item,
            item.Id,
            $"Item \"{item.Name}\" archived.",
            cancellationToken);
        return OperationResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Item archived successfully.",
        });
    }

    public async Task<OperationResult<MessageResponse>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<MessageResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var item = await itemRepository.GetByIdAsync(ownerId.Value!.Value, id, cancellationToken);
        if (item is null)
            return NotFound<MessageResponse>();

        if (await itemRepository.HasLineItemsAsync(ownerId.Value.Value, id, cancellationToken))
        {
            return OperationResult<MessageResponse>.Fail(
                "Cannot delete an item used on invoices. Archive it instead.",
                StatusCodes.Status400BadRequest);
        }

        await itemRepository.SoftDeleteAsync(ownerId.Value.Value, id, cancellationToken);
        await auditTrail.LogAsync(
            ownerId.Value.Value,
            AuditAction.Deleted,
            AuditEntityType.Item,
            item.Id,
            $"Item \"{item.Name}\" deleted.",
            cancellationToken);
        return OperationResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Item deleted successfully.",
        });
    }

    private static OperationResult<T> NotFound<T>() =>
        OperationResult<T>.Fail("Item not found.", StatusCodes.Status404NotFound);

    private static ItemResponse Map(Item item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        UnitPrice = item.UnitPrice,
        Currency = item.Currency,
        VatRate = item.VatRate,
        Category = item.Category,
        Unit = item.Unit,
        IsActive = item.IsActive,
        IsArchived = item.IsArchived,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
    };
}

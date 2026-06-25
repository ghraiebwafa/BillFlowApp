using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Repositories.Interfaces;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class ItemBillingService(
    IItemRepository itemRepository,
    ICurrentUserAccessor currentUser) : IItemBillingService
{
    public async Task<OperationResult<IReadOnlyList<ItemResponse>>> GetAllAsync(
        string? search = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<IReadOnlyList<ItemResponse>>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var items = await itemRepository.GetAllAsync(
            ownerId.Value!.Value,
            search,
            includeArchived,
            cancellationToken);

        return OperationResult<IReadOnlyList<ItemResponse>>.Ok(items.Select(Map).ToList());
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

        item.Name = request.Name.Trim();
        item.Description = request.Description?.Trim();
        item.UnitPrice = request.UnitPrice;
        item.Currency = request.Currency.Trim().ToUpperInvariant();
        item.VatRate = request.VatRate;
        item.Category = request.Category?.Trim();
        item.Unit = request.Unit?.Trim();
        item.IsActive = request.IsActive;

        await itemRepository.UpdateAsync(item, cancellationToken);
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

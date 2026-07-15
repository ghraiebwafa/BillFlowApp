using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;

namespace BillFlow.ManagementService.Services;

public interface IItemBillingService
{
    Task<OperationResult<PagedResponse<ItemResponse>>> GetAllAsync(
        string? search = null,
        bool includeArchived = false,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ItemResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ItemResponse>> CreateAsync(
        CreateItemRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ItemResponse>> UpdateAsync(
        Guid id,
        UpdateItemRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<MessageResponse>> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<MessageResponse>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

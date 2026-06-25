using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;

namespace BillFlow.ManagementService.Services;

public interface IClientBillingService
{
    Task<OperationResult<IReadOnlyList<ClientResponse>>> GetAllAsync(
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ClientResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ClientResponse>> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ClientResponse>> UpdateAsync(
        Guid id,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<MessageResponse>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

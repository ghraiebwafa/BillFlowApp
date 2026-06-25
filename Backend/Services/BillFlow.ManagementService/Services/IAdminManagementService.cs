using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Management;

namespace BillFlow.ManagementService.Services;

public interface IAdminManagementService
{
    Task<OperationResult<IReadOnlyList<UserManagementResponse>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<UserManagementResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OperationResult<UserManagementResponse>> CreateAsync(CreateAdminRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<UserManagementResponse>> UpdateAsync(Guid id, UpdateAdminRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<MessageResponse>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

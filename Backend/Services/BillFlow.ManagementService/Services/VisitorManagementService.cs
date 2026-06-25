using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Management;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Repositories.Security;
using BillFlow.Shared.Constants;

namespace BillFlow.ManagementService.Services;

public sealed class VisitorManagementService(
    IUserRepository userRepository,
    IUserSessionRevocationService sessionRevocation,
    ICurrentUserAccessor currentUser) : IVisitorManagementService
{
    public async Task<OperationResult<IReadOnlyList<UserManagementResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (!CanManageVisitors())
            return ForbiddenList();

        var visitors = await userRepository.GetAllByRoleAsync(UserRole.Visitor, cancellationToken);
        return OperationResult<IReadOnlyList<UserManagementResponse>>.Ok(
            visitors.Select(Map).ToList());
    }

    public async Task<OperationResult<UserManagementResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!CanManageVisitors())
            return Forbidden<UserManagementResponse>();

        var visitor = await userRepository.GetByIdAndRoleAsync(id, UserRole.Visitor, cancellationToken);
        if (visitor is null)
            return NotFound<UserManagementResponse>();

        return OperationResult<UserManagementResponse>.Ok(Map(visitor));
    }

    public async Task<OperationResult<UserManagementResponse>> UpdateAsync(
        Guid id,
        UpdateVisitorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CanManageVisitors())
            return Forbidden<UserManagementResponse>();

        var visitor = await userRepository.GetByIdAndRoleAsync(id, UserRole.Visitor, cancellationToken);
        if (visitor is null)
            return NotFound<UserManagementResponse>();

        var wasActive = visitor.IsActive;
        visitor.FullName = request.FullName.Trim();
        visitor.PhoneNumber = request.PhoneNumber?.Trim();
        visitor.IsActive = request.IsActive;
        await userRepository.UpdateAsync(visitor, cancellationToken);

        if (wasActive && !visitor.IsActive)
            await sessionRevocation.RevokeAllSessionsAsync(visitor.Id, cancellationToken);

        return OperationResult<UserManagementResponse>.Ok(Map(visitor));
    }

    public async Task<OperationResult<MessageResponse>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!CanManageVisitors())
            return Forbidden<MessageResponse>();

        var visitor = await userRepository.GetByIdAndRoleAsync(id, UserRole.Visitor, cancellationToken);
        if (visitor is null)
            return NotFound<MessageResponse>();

        await userRepository.SoftDeleteAsync(visitor.Id, cancellationToken);
        await sessionRevocation.RevokeAllSessionsAsync(visitor.Id, cancellationToken);

        return OperationResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Visitor account deactivated successfully.",
        });
    }

    private bool CanManageVisitors() =>
        string.Equals(currentUser.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(currentUser.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    private static OperationResult<T> Forbidden<T>() =>
        OperationResult<T>.Fail("Admin or SuperAdmin role is required.", StatusCodes.Status403Forbidden);

    private static OperationResult<IReadOnlyList<UserManagementResponse>> ForbiddenList() =>
        OperationResult<IReadOnlyList<UserManagementResponse>>.Fail(
            "Admin or SuperAdmin role is required.",
            StatusCodes.Status403Forbidden);

    private static OperationResult<T> NotFound<T>() =>
        OperationResult<T>.Fail("Resource not found.", StatusCodes.Status404NotFound);

    private static UserManagementResponse Map(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role,
        IsEmailConfirmed = user.IsEmailConfirmed,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        LastLoginAt = user.LastLoginAt,
    };
}

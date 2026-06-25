using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Management;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Repositories.Security;
using BillFlow.Shared.Constants;
using BillFlow.Shared.Security;

namespace BillFlow.ManagementService.Services;

public sealed class AdminManagementService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUserSessionRevocationService sessionRevocation,
    ICurrentUserAccessor currentUser) : IAdminManagementService
{
    public async Task<OperationResult<IReadOnlyList<UserManagementResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsSuperAdmin())
            return ForbiddenList();

        var admins = await userRepository.GetAllByRoleAsync(UserRole.Admin, cancellationToken);
        return OperationResult<IReadOnlyList<UserManagementResponse>>.Ok(
            admins.Select(Map).ToList());
    }

    public async Task<OperationResult<UserManagementResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!IsSuperAdmin())
            return Forbidden<UserManagementResponse>();

        var admin = await userRepository.GetByIdAndRoleAsync(id, UserRole.Admin, cancellationToken);
        if (admin is null)
            return NotFound<UserManagementResponse>();

        return OperationResult<UserManagementResponse>.Ok(Map(admin));
    }

    public async Task<OperationResult<UserManagementResponse>> CreateAsync(
        CreateAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsSuperAdmin())
            return Forbidden<UserManagementResponse>();

        if (await userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return OperationResult<UserManagementResponse>.Fail(
                AuthConstants.GenericAdminCreateFailureMessage,
                StatusCodes.Status400BadRequest);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Role = UserRole.Admin,
            IsEmailConfirmed = true,
            IsActive = true,
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        await userRepository.CreateAsync(user, cancellationToken);

        return OperationResult<UserManagementResponse>.Ok(Map(user), StatusCodes.Status201Created);
    }

    public async Task<OperationResult<UserManagementResponse>> UpdateAsync(
        Guid id,
        UpdateAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsSuperAdmin())
            return Forbidden<UserManagementResponse>();

        var admin = await userRepository.GetByIdAndRoleAsync(id, UserRole.Admin, cancellationToken);
        if (admin is null)
            return NotFound<UserManagementResponse>();

        var wasActive = admin.IsActive;
        admin.FullName = request.FullName.Trim();
        admin.PhoneNumber = request.PhoneNumber?.Trim();
        admin.IsActive = request.IsActive;
        await userRepository.UpdateAsync(admin, cancellationToken);

        if (wasActive && !admin.IsActive)
            await sessionRevocation.RevokeAllSessionsAsync(admin.Id, cancellationToken);

        return OperationResult<UserManagementResponse>.Ok(Map(admin));
    }

    public async Task<OperationResult<MessageResponse>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!IsSuperAdmin())
            return Forbidden<MessageResponse>();

        var admin = await userRepository.GetByIdAndRoleAsync(id, UserRole.Admin, cancellationToken);
        if (admin is null)
            return NotFound<MessageResponse>();

        if (currentUser.UserId == admin.Id)
        {
            return OperationResult<MessageResponse>.Fail(
                "You cannot delete your own account from this endpoint.",
                StatusCodes.Status400BadRequest);
        }

        await userRepository.SoftDeleteAsync(admin.Id, cancellationToken);
        await sessionRevocation.RevokeAllSessionsAsync(admin.Id, cancellationToken);

        return OperationResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Admin account deactivated successfully.",
        });
    }

    private bool IsSuperAdmin() =>
        string.Equals(currentUser.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    private static OperationResult<T> Forbidden<T>() =>
        OperationResult<T>.Fail("SuperAdmin role is required.", StatusCodes.Status403Forbidden);

    private static OperationResult<IReadOnlyList<UserManagementResponse>> ForbiddenList() =>
        OperationResult<IReadOnlyList<UserManagementResponse>>.Fail(
            "SuperAdmin role is required.",
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

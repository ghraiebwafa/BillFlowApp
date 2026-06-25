using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;

namespace BillFlow.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ConfirmEmailAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    Task HardDeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAndRoleAsync(Guid id, UserRole role, CancellationToken cancellationToken = default);

    Task<bool> AnyInRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    Task<int> GetTokenVersionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> IncrementTokenVersionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> HasBillingDataAsync(Guid userId, CancellationToken cancellationToken = default);
}

using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Users;

public sealed class UserRepository(BillFlowDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailNormalizer.Normalize(email);
        return db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailNormalizer.Normalize(email);
        return db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.Email = EmailNormalizer.Normalize(user.Email);
        user.CreatedAt = DateTime.UtcNow;

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return;

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return;

        user.IsEmailConfirmed = true;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return;

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task HardDeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return;

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default) =>
        await db.Users
            .Where(u => u.Role == role)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

    public Task<User?> GetByIdAndRoleAsync(
        Guid id,
        UserRole role,
        CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == role, cancellationToken);

    public Task<bool> AnyInRoleAsync(UserRole role, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(u => u.Role == role, cancellationToken);

    public Task<int> GetTokenVersionAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.TokenVersion)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> IncrementTokenVersionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return 1;

        user.TokenVersion++;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return user.TokenVersion;
    }

    public async Task<bool> HasBillingDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await db.Clients.AnyAsync(c => c.OwnerId == userId, cancellationToken))
            return true;

        if (await db.Items.AnyAsync(i => i.OwnerId == userId, cancellationToken))
            return true;

        return await db.Invoices.AnyAsync(i => i.OwnerId == userId, cancellationToken);
    }
}

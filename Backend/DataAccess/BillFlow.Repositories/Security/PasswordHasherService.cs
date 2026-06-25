using BillFlow.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace BillFlow.Repositories.Security;

public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string password) =>
        _hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string password, string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(user, passwordHash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

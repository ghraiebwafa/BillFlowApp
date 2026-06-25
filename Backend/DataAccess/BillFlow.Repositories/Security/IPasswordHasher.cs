using BillFlow.Models.Entities;

namespace BillFlow.Repositories.Security;

public interface IPasswordHasher
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password, string passwordHash);
}

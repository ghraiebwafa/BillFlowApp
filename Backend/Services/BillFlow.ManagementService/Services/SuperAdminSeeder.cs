using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Repositories.Security;
using BillFlow.Shared.Configuration;
using Microsoft.Extensions.Logging;

namespace BillFlow.ManagementService.Services;

public sealed class SuperAdminSeeder(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<SuperAdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await userRepository.AnyInRoleAsync(UserRole.SuperAdmin, cancellationToken))
            return;

        var email = BillFlowEnv.Require("SUPERADMIN_EMAIL");
        var password = BillFlowEnv.Require("SUPERADMIN_PASSWORD");
        var fullName = BillFlowEnv.Get("SUPERADMIN_FULL_NAME", "BillFlow Super Admin");

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            logger.LogWarning("SuperAdmin seed skipped: email {Email} already exists.", email);
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Role = UserRole.SuperAdmin,
            IsEmailConfirmed = true,
            IsActive = true,
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        await userRepository.CreateAsync(user, cancellationToken);

        logger.LogInformation("SuperAdmin account seeded for {Email}.", email);
    }
}

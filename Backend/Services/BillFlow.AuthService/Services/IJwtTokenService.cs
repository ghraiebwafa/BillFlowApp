using System.Security.Claims;
using BillFlow.Models.Entities;

namespace BillFlow.AuthService.Services;

public interface IJwtTokenService
{
    Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true);
}

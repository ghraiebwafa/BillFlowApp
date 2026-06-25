using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BillFlow.Models.Entities;
using BillFlow.Shared.Configuration;
using BillFlow.Shared.Security;
using Microsoft.IdentityModel.Tokens;

namespace BillFlow.AuthService.Services;

public sealed class JwtTokenService(
    JwtOptions options,
    ITokenSessionService tokenSession) : IJwtTokenService
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SymmetricSecurityKey _signingKey =
        new(Encoding.UTF8.GetBytes(options.Secret));

    public async Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        var tokenVersion = await tokenSession.GetTokenVersionAsync(user.Id, cancellationToken);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AuthConstants.TokenVersionClaim, tokenVersion.ToString()),
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(options.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = validateLifetime,
                    ClockSkew = TimeSpan.Zero,
                },
                out _);

            return principal;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}

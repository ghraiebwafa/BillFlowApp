using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BillFlow.AuthService.Services;

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var idValue = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(idValue, out var id) ? id : null;
        }
    }
}

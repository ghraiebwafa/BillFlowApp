using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BillFlow.Shared.Configuration;
using BillFlow.Shared.Constants;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BillFlow.Shared.Extensions;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddBillFlowJwtAuthentication(
        this IServiceCollection services,
        JwtOptions jwtOptions,
        bool requireHttpsMetadata,
        bool validateTokenVersion = false)
    {
        services.AddSingleton(jwtOptions);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.SaveToken = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(RoleNames.SuperAdmin, policy =>
                policy.RequireRole(RoleNames.SuperAdmin));

            options.AddPolicy(RoleNames.AdminOrSuperAdmin, policy =>
                policy.RequireRole(RoleNames.Admin, RoleNames.SuperAdmin));

            options.AddPolicy(RoleNames.Visitor, policy =>
                policy.RequireRole(RoleNames.Visitor));
        });

        if (validateTokenVersion)
            services.AddBillFlowJwtTokenVersionValidation();

        return services;
    }

    private static void AddBillFlowJwtTokenVersionValidation(this IServiceCollection services)
    {
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal?.Identity?.IsAuthenticated != true)
                            return;

                        var userIdValue = context.Principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                            ?? context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var versionValue = context.Principal.FindFirst(AuthConstants.TokenVersionClaim)?.Value;

                        if (!Guid.TryParse(userIdValue, out var userId)
                            || !int.TryParse(versionValue, out var tokenVersion))
                        {
                            context.Fail("Invalid token.");
                            return;
                        }

                        var tokenSession = context.HttpContext.RequestServices
                            .GetRequiredService<ITokenSessionService>();
                        var currentVersion = await tokenSession.GetTokenVersionAsync(
                            userId,
                            context.HttpContext.RequestAborted);

                        if (tokenVersion != currentVersion)
                            context.Fail("Token has been revoked.");
                    },
                };
            });
    }
}

using BillFlow.AuthService.Services;
using BillFlow.Shared.Configuration;
using BillFlow.Shared.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

namespace BillFlow.AuthService.Extensions;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddBillFlowAuthTokens(
        this IServiceCollection services,
        JwtOptions jwtOptions,
        IHostEnvironment environment)
    {
        services.AddBillFlowJwtAuthentication(
            jwtOptions,
            requireHttpsMetadata: !environment.IsDevelopment(),
            validateTokenVersion: true);
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        return services;
    }

    public static IServiceCollection AddBillFlowSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "BillFlow Auth API", Version = "v1" });
            options.AddBillFlowJwtSecurity();
        });

        return services;
    }
}

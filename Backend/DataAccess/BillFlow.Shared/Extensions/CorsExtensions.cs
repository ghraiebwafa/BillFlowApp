using BillFlow.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BillFlow.Shared.Extensions;

public static class CorsExtensions
{
    public const string DefaultPolicyName = "BillFlowCors";

    public static IServiceCollection AddBillFlowCors(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicyName, policy =>
            {
                if (environment.IsDevelopment())
                {
                    policy
                        .WithOrigins(
                            "http://localhost:3000",
                            "http://localhost:5173",
                            "http://127.0.0.1:3000",
                            "http://127.0.0.1:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    return;
                }

                var origins = BillFlowEnv.Get("CORS_ALLOWED_ORIGINS", string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (origins.Length > 0)
                {
                    policy
                        .WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        return services;
    }
}

using System.Threading.RateLimiting;
using BillFlow.Shared.Middleware;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace BillFlow.Shared.Extensions;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddBillFlowRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.AuthStrict, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, includeEmail: true),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimitPolicies.AuthModerate, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, includeEmail: true),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimitPolicies.OtpVerify, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, includeEmail: true),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimitPolicies.BillingRead, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, includeEmail: false),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimitPolicies.BillingExport, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, includeEmail: false),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }

    public static IApplicationBuilder UseBillFlowRateLimitEmailParsing(this IApplicationBuilder app) =>
        app.UseMiddleware<RateLimitEmailMiddleware>();

    private static string GetPartitionKey(HttpContext httpContext, bool includeEmail)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!includeEmail)
            return $"ip:{ip}";

        if (httpContext.Items.TryGetValue(RateLimitContextKeys.Email, out var value)
            && value is string email
            && !string.IsNullOrWhiteSpace(email))
        {
            return $"ip:{ip}|email:{email}";
        }

        return $"ip:{ip}";
    }
}

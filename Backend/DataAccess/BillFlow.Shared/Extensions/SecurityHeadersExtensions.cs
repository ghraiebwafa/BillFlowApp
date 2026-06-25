using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace BillFlow.Shared.Extensions;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseBillFlowSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Remove("Server");
            await next();
        });

        return app;
    }

    public static IApplicationBuilder UseBillFlowHstsWhenProduction(
        this IApplicationBuilder app,
        IHostEnvironment env)
    {
        if (!env.IsDevelopment())
            app.UseHsts();

        return app;
    }
}

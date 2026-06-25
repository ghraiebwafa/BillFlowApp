using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BillFlow.Shared.Extensions;

public static class HttpsRedirectionExtensions
{
    /// <summary>
    /// HTTPS redirection only in Production (TLS is expected at the reverse proxy in Docker dev).
    /// </summary>
    public static WebApplication UseBillFlowHttpsRedirection(this WebApplication app)
    {
        if (app.Environment.IsProduction())
            app.UseHttpsRedirection();

        return app;
    }
}

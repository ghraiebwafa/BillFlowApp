using System.Text.Json;
using BillFlow.Shared.Helpers;
using Microsoft.AspNetCore.Http;

namespace BillFlow.Shared.Middleware;

/// <summary>
/// Extracts email from JSON auth request bodies for per-email rate limit partitions.
/// </summary>
public sealed class RateLimitEmailMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> EmailBodyPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1.0/auth/account/login",
        "/api/v1.0/auth/account/register",
        "/api/v1.0/auth/account/resend-otp",
        "/api/v1.0/auth/account/forgot-password",
        "/api/v1.0/auth/account/resend-verification",
        "/api/v1.0/auth/account/verify-otp",
        "/api/v1.0/auth/account/reset-password",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && EmailBodyPaths.Contains(context.Request.Path.Value ?? string.Empty)
            && context.Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync(context.RequestAborted);
            context.Request.Body.Position = 0;

            TrySetEmail(context, body);
        }

        await next(context);
    }

    private static void TrySetEmail(HttpContext context, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("email", out var emailElement)
                && emailElement.ValueKind == JsonValueKind.String)
            {
                var email = emailElement.GetString();
                if (!string.IsNullOrWhiteSpace(email))
                    context.Items[RateLimitContextKeys.Email] = EmailNormalizer.Normalize(email);
            }
        }
        catch (JsonException)
        {
            // Invalid JSON — rate limit falls back to IP-only partition.
        }
    }
}

public static class RateLimitContextKeys
{
    public const string Email = "BillFlow.RateLimit.Email";
}

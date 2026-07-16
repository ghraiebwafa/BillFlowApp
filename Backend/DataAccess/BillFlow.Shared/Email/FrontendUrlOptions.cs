using BillFlow.Shared.Configuration;

namespace BillFlow.Shared.Email;

public sealed class FrontendUrlOptions
{
    public string BaseUrl { get; init; } = "http://localhost:3000";

    public static FrontendUrlOptions FromEnvironment() => new()
    {
        BaseUrl = BillFlowEnv.Get("FRONTEND_BASE_URL", "http://localhost:3000").TrimEnd('/'),
    };

    public string ResetPasswordUrl(string token) =>
        $"{BaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

    public string VerifyEmailUrl(string token) =>
        $"{BaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";
}

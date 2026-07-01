using BillFlow.Shared.Configuration;

namespace BillFlow.Shared.Email;

public sealed class SmtpOptions
{
    public string Host { get; init; } = "";

    public int Port { get; init; } = 587;

    public string? Username { get; init; }

    public string? Password { get; init; }

    public string FromEmail { get; init; } = "noreply@billflow.local";

    public string FromName { get; init; } = "BillFlow";

    public bool UseTls { get; init; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);

    public static SmtpOptions FromEnvironment() => new()
    {
        Host = BillFlowEnv.Get("SMTP_HOST", ""),
        Port = BillFlowEnv.GetInt("SMTP_PORT", 587),
        Username = NullIfEmpty(BillFlowEnv.Get("SMTP_USERNAME", "")),
        Password = NullIfEmpty(BillFlowEnv.Get("SMTP_PASSWORD", "")),
        FromEmail = BillFlowEnv.Get("SMTP_FROM_EMAIL", "noreply@billflow.local"),
        FromName = BillFlowEnv.Get("SMTP_FROM_NAME", "BillFlow"),
        UseTls = BillFlowEnv.GetBool("SMTP_USE_TLS", defaultValue: true),
    };

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}

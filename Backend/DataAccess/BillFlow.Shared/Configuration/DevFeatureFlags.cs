using Microsoft.Extensions.Hosting;

namespace BillFlow.Shared.Configuration;

public static class DevFeatureFlags
{
    /// <summary>
    /// Public reset-password without OTP/email proof. Disabled by default; enable only in local dev.
    /// Use <c>change-password</c> when authenticated in all environments.
    /// </summary>
    public static bool IsDevResetPasswordEnabled(IHostEnvironment environment) =>
        environment.IsDevelopment()
        && BillFlowEnv.GetBool("ALLOW_DEV_RESET_PASSWORD", defaultValue: false);
}

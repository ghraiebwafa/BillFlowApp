namespace BillFlow.Shared.Security;

public static class RateLimitPolicies
{
    public const string AuthStrict = "auth-strict";

    public const string AuthModerate = "auth-moderate";

    public const string OtpVerify = "otp-verify";
}

namespace BillFlow.Shared.Security;

public static class AuthConstants
{
    public const string TokenVersionClaim = "tv";

    public const string RegistrationSuccessMessage =
        "Account created successfully. You can sign in now.";

    /// <summary>
    /// Generic when REQUIRE_EMAIL_VERIFICATION is on — same for new and existing emails
    /// to avoid account enumeration.
    /// </summary>
    public const string RegistrationVerifyEmailMessage =
        "If this email is eligible, check your inbox for a verification link before signing in.";

    public const string ForgotPasswordAcceptedMessage =
        "If an account exists for that email, a reset link has been sent.";

    public const string VerificationEmailSentMessage =
        "If an unverified account exists for that email, a verification link has been sent.";

    public const string GenericAuthFailureMessage = "Invalid email or password.";

    public const string EmailNotVerifiedMessage =
        "Email address is not verified. Please verify your email before signing in.";

    public const string OtpLockoutMessage =
        "Too many verification attempts. Please try again later.";

    public const string GenericOtpFailureMessage = "Invalid or expired verification code.";

    public const string GenericAdminCreateFailureMessage =
        "Unable to create an admin with the provided details.";

    public const string GenericResetFailureMessage = "Invalid or expired reset code.";

    public const int OtpMaxAttempts = 5;

    public const int OtpLockoutMinutes = 15;
}

using Xunit;

namespace BillFlow.AuthService.Tests;

/// <summary>OTP/email flows are disabled; core auth helpers are tested here.</summary>
public sealed class AuthHelpersTests
{
    [Fact]
    public void RegistrationMessage_IndicatesImmediateLogin()
    {
        Assert.Contains("sign in", BillFlow.Shared.Security.AuthConstants.RegistrationSuccessMessage,
            StringComparison.OrdinalIgnoreCase);
    }
}

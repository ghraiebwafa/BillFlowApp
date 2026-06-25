using BillFlow.AuthService.Services;
using Xunit;

namespace BillFlow.AuthService.Tests;

public sealed class TokenHasherTests
{
    [Fact]
    public void Hash_IsDeterministic_ForSameInput()
    {
        Environment.SetEnvironmentVariable("REFRESH_TOKEN_PEPPER", "test-pepper-value-32-chars-min!!");

        var a = TokenHasher.Hash("refresh-token-plain");
        var b = TokenHasher.Hash("refresh-token-plain");

        Assert.Equal(a, b);
        Assert.NotEqual(a, TokenHasher.Hash("other-token"));
    }
}

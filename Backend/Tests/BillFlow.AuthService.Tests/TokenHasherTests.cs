using BillFlow.AuthService.Services;
using BillFlow.Shared.Configuration;
using Xunit;

namespace BillFlow.AuthService.Tests;

public sealed class TokenHasherTests
{
    private const string TestPepper = "test-pepper-value-32-chars-min!!";

    public TokenHasherTests() => TokenHasher.Configure(TestPepper);

    [Fact]
    public void Hash_IsDeterministic_ForSameInput()
    {
        var a = TokenHasher.Hash("refresh-token-plain");
        var b = TokenHasher.Hash("refresh-token-plain");

        Assert.Equal(a, b);
        Assert.NotEqual(a, TokenHasher.Hash("other-token"));
    }
}

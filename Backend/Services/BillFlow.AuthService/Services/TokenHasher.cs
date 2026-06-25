using System.Security.Cryptography;
using System.Text;
using BillFlow.Shared.Configuration;

namespace BillFlow.AuthService.Services;

public static class TokenHasher
{
    private static string? _pepper;

    public static void Configure(string pepper) => _pepper = pepper;

    public static string Hash(string token)
    {
        var pepper = _pepper ?? BillFlowEnv.Get("REFRESH_TOKEN_PEPPER", string.Empty);
        if (string.IsNullOrEmpty(pepper))
            throw new InvalidOperationException("REFRESH_TOKEN_PEPPER is not configured.");

        var payload = $"{token}{pepper}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}

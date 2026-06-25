using System.Security.Cryptography;
using System.Text;
using BillFlow.Shared.Configuration;

namespace BillFlow.AuthService.Services;

public static class TokenHasher
{
    public static string Hash(string token)
    {
        var pepper = BillFlowEnv.Get("REFRESH_TOKEN_PEPPER", string.Empty);
        var payload = string.IsNullOrEmpty(pepper) ? token : $"{token}{pepper}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}

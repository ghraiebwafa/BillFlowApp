using System.Security.Cryptography;
using System.Text;
using BillFlow.Shared.Configuration;

namespace BillFlow.Shared.Security;

public static class ShareTokenHasher
{
    public static string Hash(string token)
    {
        var pepper = BillFlowEnv.Get("REFRESH_TOKEN_PEPPER", string.Empty);
        if (string.IsNullOrEmpty(pepper))
            throw new InvalidOperationException("REFRESH_TOKEN_PEPPER is not configured.");

        var payload = $"share:{token}:{pepper}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}

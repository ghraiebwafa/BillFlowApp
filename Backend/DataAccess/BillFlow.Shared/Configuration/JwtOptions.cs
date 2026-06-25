namespace BillFlow.Shared.Configuration;

public sealed class JwtOptions
{
    public const int MinSecretLength = 32;

    public required string Secret { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 7;

    public static JwtOptions FromEnvironment()
    {
        var secret = BillFlowEnv.Require("JWT_SECRET");

        if (secret.Length < MinSecretLength)
        {
            throw new InvalidOperationException(
                $"JWT_SECRET must be at least {MinSecretLength} characters.");
        }

        return new JwtOptions
        {
            Secret = secret,
            Issuer = BillFlowEnv.Get("JWT_ISSUER", "BillFlow"),
            Audience = BillFlowEnv.Get("JWT_AUDIENCE", "BillFlow.Api"),
            AccessTokenMinutes = BillFlowEnv.GetInt("JWT_ACCESS_TOKEN_MINUTES", 15),
            RefreshTokenDays = BillFlowEnv.GetInt("JWT_REFRESH_TOKEN_DAYS", 7),
        };
    }
}

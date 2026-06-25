namespace BillFlow.Shared.Configuration;

public static class RefreshTokenPepperOptions
{
    public const int MinPepperLength = 16;

    public static string FromEnvironment()
    {
        var pepper = BillFlowEnv.Require("REFRESH_TOKEN_PEPPER");

        if (pepper.Length < MinPepperLength)
        {
            throw new InvalidOperationException(
                $"REFRESH_TOKEN_PEPPER must be at least {MinPepperLength} characters.");
        }

        return pepper;
    }
}

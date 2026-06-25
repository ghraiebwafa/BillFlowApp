namespace BillFlow.Shared.Configuration;

public static class BillFlowEnv
{
    public static string Require(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Environment variable '{key}' is required. Set it in .env or the process environment.");

        return value;
    }

    public static string Get(string key, string defaultValue) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } value
            ? value
            : defaultValue;

    public static int GetInt(string key, int defaultValue) =>
        int.TryParse(Environment.GetEnvironmentVariable(key), out var value)
            ? value
            : defaultValue;

    public static bool GetBool(string key, bool defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        return raw.Equals("true", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("1", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}

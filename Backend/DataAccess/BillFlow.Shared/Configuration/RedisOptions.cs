using StackExchange.Redis;

namespace BillFlow.Shared.Configuration;

public sealed class RedisOptions
{
    public required string Host { get; init; }

    public int Port { get; init; } = 6379;

    public required string Password { get; init; }

    public string ConnectionString
    {
        get
        {
            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                ConnectTimeout = 5000,
                SyncTimeout = 5000,
            };
            options.EndPoints.Add(Host, Port);
            options.Password = Password;
            return options.ToString();
        }
    }

    public static RedisOptions FromEnvironment() => new()
    {
        Host = BillFlowEnv.Require("REDIS_HOST"),
        Port = BillFlowEnv.GetInt("REDIS_PORT", 6379),
        Password = BillFlowEnv.Require("REDIS_PASSWORD"),
    };
}

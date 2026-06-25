using BillFlow.Shared.Configuration;
using Npgsql;

namespace BillFlow.Database.Configuration;

public static class PostgresConnection
{
    public static string FromEnvironment()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = BillFlowEnv.Require("DB_HOST"),
            Port = BillFlowEnv.GetInt("DB_PORT", 5432),
            Database = BillFlowEnv.Require("DB_NAME"),
            Username = BillFlowEnv.Require("DB_USER"),
            Password = BillFlowEnv.Require("DB_PASSWORD"),
        };

        return builder.ConnectionString;
    }

    public static string ForDesignTime()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = BillFlowEnv.Get("DB_HOST", "localhost"),
            Port = BillFlowEnv.GetInt("DB_PORT", 5432),
            Database = BillFlowEnv.Get("DB_NAME", "billflow"),
            Username = BillFlowEnv.Get("DB_USER", "billflow"),
            Password = BillFlowEnv.Get("DB_PASSWORD", "billflow"),
        };

        return builder.ConnectionString;
    }
}

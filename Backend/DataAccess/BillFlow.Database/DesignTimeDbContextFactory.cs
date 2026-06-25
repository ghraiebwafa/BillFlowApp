using BillFlow.Database.Configuration;
using BillFlow.Database.DbContexts;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BillFlow.Database;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BillFlowDbContext>
{
    public BillFlowDbContext CreateDbContext(string[] args)
    {
        Env.TraversePath().Load();

        var optionsBuilder = new DbContextOptionsBuilder<BillFlowDbContext>();
        optionsBuilder.UseNpgsql(PostgresConnection.FromEnvironment());

        return new BillFlowDbContext(optionsBuilder.Options);
    }
}

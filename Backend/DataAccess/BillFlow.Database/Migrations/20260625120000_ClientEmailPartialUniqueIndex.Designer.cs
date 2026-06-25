using BillFlow.Database.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillFlow.Database.Migrations;

[DbContext(typeof(BillFlowDbContext))]
[Migration("20260625120000_ClientEmailPartialUniqueIndex")]
public partial class ClientEmailPartialUniqueIndex
{
}

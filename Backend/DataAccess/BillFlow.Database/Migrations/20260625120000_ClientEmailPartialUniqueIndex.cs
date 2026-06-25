using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillFlow.Database.Migrations;

/// <inheritdoc />
public partial class ClientEmailPartialUniqueIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Clients_OwnerId_Email",
            table: "Clients");

        migrationBuilder.CreateIndex(
            name: "IX_Clients_OwnerId_Email",
            table: "Clients",
            columns: new[] { "OwnerId", "Email" },
            unique: true,
            filter: "\"IsDeleted\" = false");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Clients_OwnerId_Email",
            table: "Clients");

        migrationBuilder.CreateIndex(
            name: "IX_Clients_OwnerId_Email",
            table: "Clients",
            columns: new[] { "OwnerId", "Email" },
            unique: true);
    }
}

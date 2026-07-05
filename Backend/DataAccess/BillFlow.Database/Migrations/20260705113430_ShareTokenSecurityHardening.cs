using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillFlow.Database.Migrations
{
    /// <inheritdoc />
    public partial class ShareTokenSecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Token",
                table: "InvoiceShareTokens",
                newName: "TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_InvoiceShareTokens_Token",
                table: "InvoiceShareTokens",
                newName: "IX_InvoiceShareTokens_TokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "InvoiceShareTokens",
                newName: "Token");

            migrationBuilder.RenameIndex(
                name: "IX_InvoiceShareTokens_TokenHash",
                table: "InvoiceShareTokens",
                newName: "IX_InvoiceShareTokens_Token");
        }
    }
}

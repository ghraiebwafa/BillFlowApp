using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillFlow.Database.Migrations
{
    /// <inheritdoc />
    public partial class CustomerPortalInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceShareTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceShareTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceShareTokens_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceShareTokens_InvoiceId",
                table: "InvoiceShareTokens",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceShareTokens_Token",
                table: "InvoiceShareTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceShareTokens");
        }
    }
}

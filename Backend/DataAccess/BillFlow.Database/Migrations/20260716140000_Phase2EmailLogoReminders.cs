using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillFlow.Database.Migrations;

/// <inheritdoc />
public partial class Phase2EmailLogoReminders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EnablePaymentReminders",
            table: "CompanySettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<byte[]>(
            name: "LogoBytes",
            table: "CompanySettings",
            type: "bytea",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LogoContentType",
            table: "CompanySettings",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ReminderDaysBeforeDue",
            table: "CompanySettings",
            type: "integer",
            nullable: false,
            defaultValue: 3);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastPaymentReminderSentAt",
            table: "Invoices",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "AuthEmailTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Purpose = table.Column<int>(type: "integer", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuthEmailTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuthEmailTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuthEmailTokens_TokenHash_Purpose",
            table: "AuthEmailTokens",
            columns: new[] { "TokenHash", "Purpose" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuthEmailTokens_UserId_Purpose_ExpiresAt",
            table: "AuthEmailTokens",
            columns: new[] { "UserId", "Purpose", "ExpiresAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuthEmailTokens");

        migrationBuilder.DropColumn(name: "EnablePaymentReminders", table: "CompanySettings");
        migrationBuilder.DropColumn(name: "LogoBytes", table: "CompanySettings");
        migrationBuilder.DropColumn(name: "LogoContentType", table: "CompanySettings");
        migrationBuilder.DropColumn(name: "ReminderDaysBeforeDue", table: "CompanySettings");
        migrationBuilder.DropColumn(name: "LastPaymentReminderSentAt", table: "Invoices");
    }
}

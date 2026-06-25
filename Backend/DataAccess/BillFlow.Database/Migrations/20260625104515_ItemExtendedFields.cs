using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillFlow.Database.Migrations
{
    /// <inheritdoc />
    public partial class ItemExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "Items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Items");
        }
    }
}

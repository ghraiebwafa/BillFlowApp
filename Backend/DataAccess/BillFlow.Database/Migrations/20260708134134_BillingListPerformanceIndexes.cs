using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillFlow.Database.Migrations
{
    /// <inheritdoc />
    public partial class BillingListPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_OwnerId",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OwnerId_Status_PaymentDate",
                table: "Payments",
                columns: new[] { "OwnerId", "Status", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_OwnerId_Name",
                table: "Items",
                columns: new[] { "OwnerId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OwnerId_InvoiceDate",
                table: "Invoices",
                columns: new[] { "OwnerId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OwnerId_Status",
                table: "Invoices",
                columns: new[] { "OwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status_DueDate",
                table: "Invoices",
                columns: new[] { "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_OwnerId_Status_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Items_OwnerId_Name",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_OwnerId_InvoiceDate",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_OwnerId_Status",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Status_DueDate",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Items_OwnerId",
                table: "Items",
                column: "OwnerId");
        }
    }
}

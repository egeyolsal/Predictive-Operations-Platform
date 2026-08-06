using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskInventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceLineItems_InvoiceId",
                table: "InvoiceLineItems");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId_InventoryItemId",
                table: "InvoiceLineItems",
                columns: new[] { "InvoiceId", "InventoryItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceLineItems_InvoiceId_InventoryItemId",
                table: "InvoiceLineItems");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId",
                table: "InvoiceLineItems",
                column: "InvoiceId");
        }
    }
}

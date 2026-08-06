using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskInventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class Phase1SupplierExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Suppliers_SupplierId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SupplierId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "InventoryItems");

            migrationBuilder.CreateTable(
                name: "ItemSuppliers",
                columns: table => new
                {
                    InventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSuppliers", x => new { x.InventoryItemId, x.SupplierId });
                    table.ForeignKey(
                        name: "FK_ItemSuppliers_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemSuppliers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSuppliers_SupplierId",
                table: "ItemSuppliers",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemSuppliers");

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SupplierId",
                table: "InventoryItems",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Suppliers_SupplierId",
                table: "InventoryItems",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

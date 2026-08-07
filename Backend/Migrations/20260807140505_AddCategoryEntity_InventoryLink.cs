using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskInventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryEntity_InventoryLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "InventoryItems");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Categories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CategoryId",
                table: "InventoryItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Categories_CategoryId",
                table: "InventoryItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Categories_CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "InventoryItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}

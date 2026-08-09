using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskInventoryAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSystemGeneratedAndRelatedItemToTaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemGenerated",
                table: "TaskItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RelatedInventoryItemId",
                table: "TaskItems",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystemGenerated",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "RelatedInventoryItemId",
                table: "TaskItems");
        }
    }
}

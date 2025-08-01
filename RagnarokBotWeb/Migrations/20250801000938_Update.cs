using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarzoneItems_Items_ItemId",
                table: "WarzoneItems");

            migrationBuilder.AddForeignKey(
                name: "FK_WarzoneItems_Items_ItemId",
                table: "WarzoneItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarzoneItems_Items_ItemId",
                table: "WarzoneItems");

            migrationBuilder.AddForeignKey(
                name: "FK_WarzoneItems_Items_ItemId",
                table: "WarzoneItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKillFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HideMineKill",
                table: "ScumServers",
                newName: "ShowMineKill");

            migrationBuilder.RenameColumn(
                name: "HideKillerName",
                table: "ScumServers",
                newName: "ShowKillerName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShowMineKill",
                table: "ScumServers",
                newName: "HideMineKill");

            migrationBuilder.RenameColumn(
                name: "ShowKillerName",
                table: "ScumServers",
                newName: "HideKillerName");
        }
    }
}

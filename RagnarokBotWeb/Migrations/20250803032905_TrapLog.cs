using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class TrapLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowMinesOutsideFlag",
                table: "ScumServers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AnnounceMineOutsideFlag",
                table: "ScumServers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "CoinReductionPerInvalidMineKill",
                table: "ScumServers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowMinesOutsideFlag",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "AnnounceMineOutsideFlag",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "CoinReductionPerInvalidMineKill",
                table: "ScumServers");
        }
    }
}

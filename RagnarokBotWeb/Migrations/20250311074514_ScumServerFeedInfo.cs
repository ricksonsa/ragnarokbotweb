using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class ScumServerFeedInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideKillerName",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HideMineKill",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendVipLockpickAlert",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowKillDistance",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowKillSector",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowKillWeapon",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowLockpickContainerName",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowLockpickSector",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowSameSquadKill",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseKillFeed",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseLockpickFeed",
                table: "ScumServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HideKillerName",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "HideMineKill",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "SendVipLockpickAlert",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "ShowKillDistance",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "ShowKillSector",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "ShowKillWeapon",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "ShowLockpickContainerName",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "ShowLockpickSector",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "ShowSameSquadKill",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "UseKillFeed",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "UseLockpickFeed",
                table: "ScumServers");
        }
    }
}

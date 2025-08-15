using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDateBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "WarzoneTeleports",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "WarzoneSpawns",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Warzones",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "WarzoneItems",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Vips",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Uavs",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Transactions",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Tenants",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Teleports",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Subscriptions",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Silences",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "ScumServers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "ScheduledTasks",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "ReaderPointers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Players",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "PlayerRegisters",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Payments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Packs",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "PackItems",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Lockpicks",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Kills",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Items",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Guilds",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Ftps",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "DiscordRoles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Config",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "ChannelTemplates",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Channels",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "ButtonTemplates",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Buttons",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Bunkers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Blocks",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Bans",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "WarzoneTeleports");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "WarzoneSpawns");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Warzones");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "WarzoneItems");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Vips");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Uavs");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Teleports");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Silences");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "ReaderPointers");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "PlayerRegisters");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Packs");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "PackItems");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Lockpicks");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Kills");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Ftps");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "DiscordRoles");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "ChannelTemplates");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "ButtonTemplates");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Buttons");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Bunkers");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Bans");
        }
    }
}

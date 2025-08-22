using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class TaxiAndExchange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Packs_ScumServers_ScumServerId",
                table: "Packs");

            migrationBuilder.DropForeignKey(
                name: "FK_Warzones_ScumServers_ScumServerId",
                table: "Warzones");

            migrationBuilder.DropColumn(
                name: "DiscordId",
                table: "Uavs");

            migrationBuilder.DropColumn(
                name: "Commands",
                table: "Packs");

            migrationBuilder.AlterColumn<long>(
                name: "ScumServerId",
                table: "Warzones",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted",
                table: "Uavs",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscordChannelId",
                table: "Uavs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MinPlayerOnline",
                table: "Uavs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ScumServerId",
                table: "Uavs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExchangeId",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ScumServerId",
                table: "Packs",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "MinPlayerOnline",
                table: "Packs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TaxiId",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Exchanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DeliveryText = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    VipPrice = table.Column<long>(type: "bigint", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    DiscordChannelId = table.Column<string>(type: "text", nullable: true),
                    DiscordMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    PurchaseCooldownSeconds = table.Column<long>(type: "bigint", nullable: true),
                    MinPlayerOnline = table.Column<long>(type: "bigint", nullable: true),
                    StockPerPlayer = table.Column<long>(type: "bigint", nullable: true),
                    StockPerVipPlayer = table.Column<long>(type: "bigint", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsBlockPurchaseRaidTime = table.Column<bool>(type: "boolean", nullable: false),
                    IsVipOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exchanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Taxis",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DeliveryText = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    VipPrice = table.Column<long>(type: "bigint", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    DiscordChannelId = table.Column<string>(type: "text", nullable: true),
                    DiscordMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    PurchaseCooldownSeconds = table.Column<long>(type: "bigint", nullable: true),
                    MinPlayerOnline = table.Column<long>(type: "bigint", nullable: true),
                    StockPerPlayer = table.Column<long>(type: "bigint", nullable: true),
                    StockPerVipPlayer = table.Column<long>(type: "bigint", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsBlockPurchaseRaidTime = table.Column<bool>(type: "boolean", nullable: false),
                    IsVipOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taxis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Taxis_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaxiTeleports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaxiId = table.Column<long>(type: "bigint", nullable: false),
                    TeleportId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxiTeleports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxiTeleports_Taxis_TaxiId",
                        column: x => x.TaxiId,
                        principalTable: "Taxis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaxiTeleports_Teleports_TeleportId",
                        column: x => x.TeleportId,
                        principalTable: "Teleports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScumServers_ExchangeId",
                table: "ScumServers",
                column: "ExchangeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TaxiId",
                table: "Orders",
                column: "TaxiId");

            migrationBuilder.CreateIndex(
                name: "IX_Taxis_ScumServerId",
                table: "Taxis",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxiTeleports_TaxiId",
                table: "TaxiTeleports",
                column: "TaxiId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxiTeleports_TeleportId",
                table: "TaxiTeleports",
                column: "TeleportId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Taxis_TaxiId",
                table: "Orders",
                column: "TaxiId",
                principalTable: "Taxis",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Packs_ScumServers_ScumServerId",
                table: "Packs",
                column: "ScumServerId",
                principalTable: "ScumServers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScumServers_Exchanges_ExchangeId",
                table: "ScumServers",
                column: "ExchangeId",
                principalTable: "Exchanges",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warzones_ScumServers_ScumServerId",
                table: "Warzones",
                column: "ScumServerId",
                principalTable: "ScumServers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Taxis_TaxiId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Packs_ScumServers_ScumServerId",
                table: "Packs");

            migrationBuilder.DropForeignKey(
                name: "FK_ScumServers_Exchanges_ExchangeId",
                table: "ScumServers");

            migrationBuilder.DropForeignKey(
                name: "FK_Warzones_ScumServers_ScumServerId",
                table: "Warzones");

            migrationBuilder.DropTable(
                name: "Exchanges");

            migrationBuilder.DropTable(
                name: "TaxiTeleports");

            migrationBuilder.DropTable(
                name: "Taxis");

            migrationBuilder.DropIndex(
                name: "IX_ScumServers_ExchangeId",
                table: "ScumServers");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TaxiId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Uavs");

            migrationBuilder.DropColumn(
                name: "DiscordChannelId",
                table: "Uavs");

            migrationBuilder.DropColumn(
                name: "MinPlayerOnline",
                table: "Uavs");

            migrationBuilder.DropColumn(
                name: "ScumServerId",
                table: "Uavs");

            migrationBuilder.DropColumn(
                name: "ExchangeId",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "MinPlayerOnline",
                table: "Packs");

            migrationBuilder.DropColumn(
                name: "TaxiId",
                table: "Orders");

            migrationBuilder.AlterColumn<long>(
                name: "ScumServerId",
                table: "Warzones",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscordId",
                table: "Uavs",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ScumServerId",
                table: "Packs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Commands",
                table: "Packs",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Packs_ScumServers_ScumServerId",
                table: "Packs",
                column: "ScumServerId",
                principalTable: "ScumServers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Warzones_ScumServers_ScumServerId",
                table: "Warzones",
                column: "ScumServerId",
                principalTable: "ScumServers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

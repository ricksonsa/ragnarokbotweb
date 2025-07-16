using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class Warzone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderType",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "WarzoneId",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Warzones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DeliveryText = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    VipPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    DiscordChannelId = table.Column<string>(type: "text", nullable: true),
                    DiscordMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    PurchaseCooldownSeconds = table.Column<long>(type: "bigint", nullable: true),
                    StockPerPlayer = table.Column<long>(type: "bigint", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsBlockPurchaseRaidTime = table.Column<bool>(type: "boolean", nullable: false),
                    IsVipOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    Deleted = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warzones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warzones_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teleports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Coordinates = table.Column<string>(type: "text", nullable: false),
                    WarzoneId = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teleports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teleports_Warzones_WarzoneId",
                        column: x => x.WarzoneId,
                        principalTable: "Warzones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WarzoneItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    WarzoneId = table.Column<long>(type: "bigint", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Deleted = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarzoneItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarzoneItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarzoneItems_Warzones_WarzoneId",
                        column: x => x.WarzoneId,
                        principalTable: "Warzones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_WarzoneId",
                table: "Orders",
                column: "WarzoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Teleports_WarzoneId",
                table: "Teleports",
                column: "WarzoneId");

            migrationBuilder.CreateIndex(
                name: "IX_WarzoneItems_ItemId",
                table: "WarzoneItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WarzoneItems_WarzoneId",
                table: "WarzoneItems",
                column: "WarzoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Warzones_ScumServerId",
                table: "Warzones",
                column: "ScumServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Warzones_WarzoneId",
                table: "Orders",
                column: "WarzoneId",
                principalTable: "Warzones",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Warzones_WarzoneId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Teleports");

            migrationBuilder.DropTable(
                name: "WarzoneItems");

            migrationBuilder.DropTable(
                name: "Warzones");

            migrationBuilder.DropIndex(
                name: "IX_Orders_WarzoneId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WarzoneId",
                table: "Orders");
        }
    }
}

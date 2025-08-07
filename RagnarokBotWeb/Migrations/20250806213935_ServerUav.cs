using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class ServerUav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Warzones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<long>(
                name: "UavId",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Packs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Uav",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Uavs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DeliveryText = table.Column<string>(type: "text", nullable: true),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    DiscordMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    VipPrice = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseCooldownSeconds = table.Column<long>(type: "bigint", nullable: true),
                    StockPerPlayer = table.Column<long>(type: "bigint", nullable: true),
                    StockPerVipPlayer = table.Column<long>(type: "bigint", nullable: true),
                    IsBlockPurchaseRaidTime = table.Column<bool>(type: "boolean", nullable: false),
                    IsVipOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uavs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScumServers_UavId",
                table: "ScumServers",
                column: "UavId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScumServers_Uavs_UavId",
                table: "ScumServers",
                column: "UavId",
                principalTable: "Uavs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScumServers_Uavs_UavId",
                table: "ScumServers");

            migrationBuilder.DropTable(
                name: "Uavs");

            migrationBuilder.DropIndex(
                name: "IX_ScumServers_UavId",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "UavId",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "Uav",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Warzones",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Packs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}

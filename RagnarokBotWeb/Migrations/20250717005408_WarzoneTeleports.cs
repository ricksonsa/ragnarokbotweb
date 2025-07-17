using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class WarzoneTeleports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teleports_Warzones_WarzoneId",
                table: "Teleports");

            migrationBuilder.DropIndex(
                name: "IX_Teleports_WarzoneId",
                table: "Teleports");

            migrationBuilder.DropColumn(
                name: "WarzoneId",
                table: "Teleports");

            migrationBuilder.AlterColumn<long>(
                name: "VipPrice",
                table: "Warzones",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<long>(
                name: "Price",
                table: "Warzones",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<long>(
                name: "ItemSpawnInterval",
                table: "Warzones",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WarzoneDurationInterval",
                table: "Warzones",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "WarzoneSpawns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarzoneId = table.Column<long>(type: "bigint", nullable: false),
                    TeleportId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarzoneSpawns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarzoneSpawns_Teleports_TeleportId",
                        column: x => x.TeleportId,
                        principalTable: "Teleports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarzoneSpawns_Warzones_WarzoneId",
                        column: x => x.WarzoneId,
                        principalTable: "Warzones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarzoneTeleports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarzoneId = table.Column<long>(type: "bigint", nullable: false),
                    TeleportId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarzoneTeleports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarzoneTeleports_Teleports_TeleportId",
                        column: x => x.TeleportId,
                        principalTable: "Teleports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarzoneTeleports_Warzones_WarzoneId",
                        column: x => x.WarzoneId,
                        principalTable: "Warzones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarzoneSpawns_TeleportId",
                table: "WarzoneSpawns",
                column: "TeleportId");

            migrationBuilder.CreateIndex(
                name: "IX_WarzoneSpawns_WarzoneId",
                table: "WarzoneSpawns",
                column: "WarzoneId");

            migrationBuilder.CreateIndex(
                name: "IX_WarzoneTeleports_TeleportId",
                table: "WarzoneTeleports",
                column: "TeleportId");

            migrationBuilder.CreateIndex(
                name: "IX_WarzoneTeleports_WarzoneId",
                table: "WarzoneTeleports",
                column: "WarzoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarzoneSpawns");

            migrationBuilder.DropTable(
                name: "WarzoneTeleports");

            migrationBuilder.DropColumn(
                name: "ItemSpawnInterval",
                table: "Warzones");

            migrationBuilder.DropColumn(
                name: "WarzoneDurationInterval",
                table: "Warzones");

            migrationBuilder.AlterColumn<decimal>(
                name: "VipPrice",
                table: "Warzones",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Warzones",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "WarzoneId",
                table: "Teleports",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teleports_WarzoneId",
                table: "Teleports",
                column: "WarzoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teleports_Warzones_WarzoneId",
                table: "Teleports",
                column: "WarzoneId",
                principalTable: "Warzones",
                principalColumn: "Id");
        }
    }
}

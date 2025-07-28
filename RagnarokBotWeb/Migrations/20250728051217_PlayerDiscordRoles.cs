using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class PlayerDiscordRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Processed",
                table: "Vips",
                newName: "Indefinitely");

            migrationBuilder.RenameColumn(
                name: "Processed",
                table: "Silences",
                newName: "Indefinitely");

            migrationBuilder.RenameColumn(
                name: "Processed",
                table: "Bans",
                newName: "Indefinitely");

            migrationBuilder.CreateTable(
                name: "DiscordRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Indefinitely = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerId = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordRoles_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordRoles_PlayerId",
                table: "DiscordRoles",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordRoles");

            migrationBuilder.RenameColumn(
                name: "Indefinitely",
                table: "Vips",
                newName: "Processed");

            migrationBuilder.RenameColumn(
                name: "Indefinitely",
                table: "Silences",
                newName: "Processed");

            migrationBuilder.RenameColumn(
                name: "Indefinitely",
                table: "Bans",
                newName: "Processed");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bunkers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sector = table.Column<string>(type: "TEXT", nullable: false),
                    Locked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Available = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bunkers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Readings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    SteamId64 = table.Column<string>(type: "TEXT", nullable: true),
                    ScumId = table.Column<string>(type: "TEXT", nullable: true),
                    DiscordId = table.Column<string>(type: "TEXT", nullable: true),
                    Presence = table.Column<string>(type: "TEXT", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lockpicks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LockType = table.Column<string>(type: "TEXT", nullable: false),
                    SteamId64 = table.Column<string>(type: "TEXT", nullable: false),
                    ScumId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AttemptDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lockpicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lockpicks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lockpicks_UserId",
                table: "Lockpicks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bunkers");

            migrationBuilder.DropTable(
                name: "Lockpicks");

            migrationBuilder.DropTable(
                name: "Readings");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

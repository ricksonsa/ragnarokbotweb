using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class reader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hash",
                table: "Readings");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "ScumServers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Processed",
                table: "Readings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ScumServerId",
                table: "Readings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ReaderPointers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FileType = table.Column<int>(type: "INTEGER", nullable: false),
                    ScumServerId = table.Column<long>(type: "INTEGER", nullable: false),
                    FileDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaderPointers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReaderPointers_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Readings_ScumServerId",
                table: "Readings",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReaderPointers_ScumServerId",
                table: "ReaderPointers",
                column: "ScumServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Readings_ScumServers_ScumServerId",
                table: "Readings",
                column: "ScumServerId",
                principalTable: "ScumServers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Readings_ScumServers_ScumServerId",
                table: "Readings");

            migrationBuilder.DropTable(
                name: "ReaderPointers");

            migrationBuilder.DropIndex(
                name: "IX_Readings_ScumServerId",
                table: "Readings");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "Processed",
                table: "Readings");

            migrationBuilder.DropColumn(
                name: "ScumServerId",
                table: "Readings");

            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "Readings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}

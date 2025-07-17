using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class WarzoneRemoveIsRunning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRunning",
                table: "Warzones");

            migrationBuilder.AddColumn<DateTime>(
                name: "StopAt",
                table: "Warzones",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopAt",
                table: "Warzones");

            migrationBuilder.AddColumn<bool>(
                name: "IsRunning",
                table: "Warzones",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class CustomTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Cron = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsBlockPurchaseRaidTime = table.Column<bool>(type: "boolean", nullable: false),
                    StartMessage = table.Column<string>(type: "text", nullable: true),
                    MinPlayerOnline = table.Column<long>(type: "bigint", nullable: true),
                    LastRunned = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TaskType = table.Column<int>(type: "integer", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: true),
                    Commands = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomTasks_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomTasks_ScumServerId",
                table: "CustomTasks",
                column: "ScumServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomTasks");
        }
    }
}

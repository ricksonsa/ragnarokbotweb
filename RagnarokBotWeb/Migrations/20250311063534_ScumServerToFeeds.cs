using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class ScumServerToFeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kills_Players_KillerId",
                table: "Kills");

            migrationBuilder.DropForeignKey(
                name: "FK_Kills_Players_TargetId",
                table: "Kills");

            migrationBuilder.DropForeignKey(
                name: "FK_Lockpicks_Players_UserId",
                table: "Lockpicks");

            migrationBuilder.DropIndex(
                name: "IX_Lockpicks_UserId",
                table: "Lockpicks");

            migrationBuilder.DropIndex(
                name: "IX_Kills_KillerId",
                table: "Kills");

            migrationBuilder.DropIndex(
                name: "IX_Kills_TargetId",
                table: "Kills");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Lockpicks");

            migrationBuilder.DropColumn(
                name: "KillerId",
                table: "Kills");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "Kills");

            migrationBuilder.AddColumn<long>(
                name: "ScumServerId",
                table: "Lockpicks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ScumServerId",
                table: "Kills",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Lockpicks_ScumServerId",
                table: "Lockpicks",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Kills_ScumServerId",
                table: "Kills",
                column: "ScumServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kills_ScumServers_ScumServerId",
                table: "Kills",
                column: "ScumServerId",
                principalTable: "ScumServers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lockpicks_ScumServers_ScumServerId",
                table: "Lockpicks",
                column: "ScumServerId",
                principalTable: "ScumServers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kills_ScumServers_ScumServerId",
                table: "Kills");

            migrationBuilder.DropForeignKey(
                name: "FK_Lockpicks_ScumServers_ScumServerId",
                table: "Lockpicks");

            migrationBuilder.DropIndex(
                name: "IX_Lockpicks_ScumServerId",
                table: "Lockpicks");

            migrationBuilder.DropIndex(
                name: "IX_Kills_ScumServerId",
                table: "Kills");

            migrationBuilder.DropColumn(
                name: "ScumServerId",
                table: "Lockpicks");

            migrationBuilder.DropColumn(
                name: "ScumServerId",
                table: "Kills");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Lockpicks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillerId",
                table: "Kills",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TargetId",
                table: "Kills",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lockpicks_UserId",
                table: "Lockpicks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Kills_KillerId",
                table: "Kills",
                column: "KillerId");

            migrationBuilder.CreateIndex(
                name: "IX_Kills_TargetId",
                table: "Kills",
                column: "TargetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kills_Players_KillerId",
                table: "Kills",
                column: "KillerId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kills_Players_TargetId",
                table: "Kills",
                column: "TargetId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Lockpicks_Players_UserId",
                table: "Lockpicks",
                column: "UserId",
                principalTable: "Players",
                principalColumn: "Id");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledTasksInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BlockedRaidTimes",
                table: "ScheduledTasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ScheduledTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "ScheduledTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduledTaskType",
                table: "ScheduledTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "ScheduledTasks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockedRaidTimes",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "ScheduledTaskType",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ScheduledTasks");
        }
    }
}

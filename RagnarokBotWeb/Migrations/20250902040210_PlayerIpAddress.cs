using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class PlayerIpAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Players",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Players");
        }
    }
}

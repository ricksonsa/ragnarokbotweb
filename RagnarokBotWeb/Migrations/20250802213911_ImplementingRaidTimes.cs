using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class ImplementingRaidTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "VipPrice",
                table: "Packs",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<long>(
                name: "Price",
                table: "Packs",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "VipPrice",
                table: "Packs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Packs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}

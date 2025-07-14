using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddFileSizeReaderPointer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileType",
                table: "ReaderPointers");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "ReaderPointers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "ReaderPointers");

            migrationBuilder.AddColumn<int>(
                name: "FileType",
                table: "ReaderPointers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

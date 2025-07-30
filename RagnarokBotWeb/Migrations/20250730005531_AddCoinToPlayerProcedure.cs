using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCoinToPlayerProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CoinAwardPeriodically",
                table: "ScumServers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION addcoinstoplayer(player_id BIGINT, amount BIGINT)
                    RETURNS VOID AS $$
                    BEGIN
                        UPDATE ""Players""
                        SET ""Coin"" = ""Coin"" + amount
                        WHERE ""Id"" = player_id;
                    END;
                    $$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoinAwardPeriodically",
                table: "ScumServers");

            migrationBuilder.Sql("DROP FUNCTION addcoinstoplayer");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlayerCoinFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION reducecoinstoplayer(player_id BIGINT, amount BIGINT)
                    RETURNS VOID AS $$
                    BEGIN
                        UPDATE ""Players""
                        SET ""Coin"" = ""Coin"" - amount
                        WHERE ""Id"" = player_id;
                    END;
                    $$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION reducecoinstoplayer");
        }
    }
}

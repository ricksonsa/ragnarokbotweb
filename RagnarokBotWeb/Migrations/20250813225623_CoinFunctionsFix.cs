using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class CoinFunctionsFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION addcoinstoplayerbysteamid");
            migrationBuilder.Sql("DROP FUNCTION reducestoplayerbysteamid");

            migrationBuilder.Sql(@"
                 CREATE OR REPLACE FUNCTION addcoinstoplayerbysteamid(steam_id TEXT, server_id BIGINT, amount BIGINT)
                     RETURNS VOID AS $$
                     BEGIN
                         UPDATE ""Players""
                         SET ""Coin"" = ""Coin"" + amount
                         WHERE ""SteamId64"" = steam_id
                         AND ""ScumServerId"" = server_id;
                     END;
                     $$ LANGUAGE plpgsql;
             ");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION reducetoplayerbysteamid(steam_id TEXT, server_id BIGINT, amount BIGINT)
                    RETURNS VOID AS $$
                    BEGIN
                        UPDATE ""Players""
                        SET ""Coin"" = ""Coin"" - amount
                        WHERE ""SteamId64"" = steam_id
                        AND ""ScumServerId"" = server_id;
                    END;
                    $$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION addcoinstoplayerbysteamid");
            migrationBuilder.Sql("DROP FUNCTION reducetoplayerbysteamid");
        }
    }
}

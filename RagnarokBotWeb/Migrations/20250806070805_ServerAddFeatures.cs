using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class ServerAddFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StockPerVipPlayer",
                table: "Warzones",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CoinAwardIntervalMinutes",
                table: "ScumServers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CoinDeathPenaltyAmount",
                table: "ScumServers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CoinKillAwardAmount",
                table: "ScumServers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "ShopEnabled",
                table: "ScumServers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "WelcomePackCoinAward",
                table: "ScumServers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "StockPerVipPlayer",
                table: "Packs",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(@"
                 CREATE OR REPLACE FUNCTION addcoinstoplayerbysteamid(steam_id BIGINT, server_id BIGINT, amount BIGINT)
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
                CREATE OR REPLACE FUNCTION reducestoplayerbysteamid(steam_id BIGINT, server_id BIGINT, amount BIGINT)
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
            migrationBuilder.DropColumn(
                name: "StockPerVipPlayer",
                table: "Warzones");

            migrationBuilder.DropColumn(
                name: "CoinAwardIntervalMinutes",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "CoinDeathPenaltyAmount",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "CoinKillAwardAmount",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "ShopEnabled",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "WelcomePackCoinAward",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "StockPerVipPlayer",
                table: "Packs");

            migrationBuilder.Sql("DROP FUNCTION addcoinstoplayerbysteamid");
            migrationBuilder.Sql("DROP FUNCTION reducestoplayerbysteamid");
        }
    }
}

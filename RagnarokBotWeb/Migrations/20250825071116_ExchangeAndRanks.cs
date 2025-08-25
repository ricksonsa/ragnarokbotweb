using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class ExchangeAndRanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "KillRankDailyTop1Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankDailyTop2Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankDailyTop3Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankDailyTop4Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankDailyTop5Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankMonthlyTop1Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankMonthlyTop2Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankMonthlyTop3Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankMonthlyTop4Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankMonthlyTop5Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankWeeklyTop1Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankWeeklyTop2Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankWeeklyTop3Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankWeeklyTop4Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KillRankWeeklyTop5Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LockpickRankDailyTop1Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LockpickRankDailyTop2Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LockpickRankDailyTop3Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LockpickRankDailyTop4Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LockpickRankDailyTop5Award",
                table: "ScumServers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExchangeAmount",
                table: "Orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ExchangeType",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Rankable",
                table: "Kills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowDeposit",
                table: "Exchanges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowTransfer",
                table: "Exchanges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowWithdraw",
                table: "Exchanges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "DepositRate",
                table: "Exchanges",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TransferRate",
                table: "Exchanges",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WithdrawRate",
                table: "Exchanges",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KillRankDailyTop1Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankDailyTop2Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankDailyTop3Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankDailyTop4Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankDailyTop5Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankMonthlyTop1Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankMonthlyTop2Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankMonthlyTop3Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankMonthlyTop4Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankMonthlyTop5Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankWeeklyTop1Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankWeeklyTop2Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankWeeklyTop3Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankWeeklyTop4Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "KillRankWeeklyTop5Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "LockpickRankDailyTop1Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "LockpickRankDailyTop2Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "LockpickRankDailyTop3Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "LockpickRankDailyTop4Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "LockpickRankDailyTop5Award",
                table: "ScumServers");

            migrationBuilder.DropColumn(
                name: "ExchangeAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Rankable",
                table: "Kills");

            migrationBuilder.DropColumn(
                name: "AllowDeposit",
                table: "Exchanges");

            migrationBuilder.DropColumn(
                name: "AllowTransfer",
                table: "Exchanges");

            migrationBuilder.DropColumn(
                name: "AllowWithdraw",
                table: "Exchanges");

            migrationBuilder.DropColumn(
                name: "DepositRate",
                table: "Exchanges");

            migrationBuilder.DropColumn(
                name: "TransferRate",
                table: "Exchanges");

            migrationBuilder.DropColumn(
                name: "WithdrawRate",
                table: "Exchanges");
        }
    }
}

using Discord;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class KillRankWeeklyAwardJob(
        ILogger<KillRankWeeklyAwardJob> logger,
        IScumServerRepository scumServerRepository,
        IDiscordService discordService,
        IUnitOfWork unitOfWork
        ) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(context);
                var topKillersMonthly = await TopPlayers(unitOfWork, server, ERankingPeriod.Monthly);

                var manager = new PlayerCoinManager(unitOfWork);
                await HandleAwardsWeekly(unitOfWork, discordService, server, topKillersMonthly, manager);
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task HandleAwardsWeekly(
            IUnitOfWork uow,
            IDiscordService discordService,
            ScumServer server,
            List<PlayerStatsDto> topKillers,
            PlayerCoinManager manager)
        {
            if (server.KillRankWeeklyTop1Award.HasValue && server.KillRankWeeklyTop1Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topKillers[0].SteamId && p.ScumServerId == server.Id);
                var amount = server.KillRankWeeklyTop1Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 1 spot in the Weekly Kill Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
            }

            if (server.KillRankWeeklyTop2Award.HasValue && server.KillRankWeeklyTop2Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topKillers[0].SteamId && p.ScumServerId == server.Id);
                var amount = server.KillRankWeeklyTop2Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 2 spot in the Weekly Kill Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
            }

            if (server.KillRankWeeklyTop3Award.HasValue && server.KillRankWeeklyTop3Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topKillers[0].SteamId && p.ScumServerId == server.Id);
                var amount = server.KillRankWeeklyTop3Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 3 spot in the Weekly Kill Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
            }

            if (server.KillRankWeeklyTop4Award.HasValue && server.KillRankWeeklyTop4Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topKillers[0].SteamId && p.ScumServerId == server.Id);
                var amount = server.KillRankWeeklyTop4Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 4 spot in the Weekly Kill Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
            }

            if (server.KillRankWeeklyTop5Award.HasValue && server.KillRankWeeklyTop5Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topKillers[0].SteamId && p.ScumServerId == server.Id);
                var amount = server.KillRankWeeklyTop5Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 5 spot in the Weekly Kill Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
            }
        }

        private static DateTime GetStartOfWeek(DateTime date, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.Date.AddDays(-diff);
        }

        private static DateTime GetStartOfMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        private static DateTime GetToday(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day);
        }

        private static async Task<List<PlayerStatsDto>> TopPlayers(
          IUnitOfWork unitOfWork,
          ScumServer server,
          ERankingPeriod period,
          int topCount = 5)
        {
            // Determine the starting point of the ranking period
            var now = DateTime.UtcNow;
            DateTime periodStart = period switch
            {
                ERankingPeriod.Daily => GetToday(now),
                ERankingPeriod.Weekly => GetStartOfWeek(now, DayOfWeek.Monday),
                ERankingPeriod.Monthly => GetStartOfMonth(now),
                _ => now.Date
            };

            // Filter kills by server and period
            var kills = unitOfWork.Kills
                .Include(kill => kill.ScumServer)
                .Where(k => k.ScumServer.Id == server.Id
                    && k.CreateDate >= periodStart
                    && k.KillerSteamId64 != "-1"
                    && !k.IsSameSquad
                    && k.Rankable);

            // Group by KillerName
            var killerStats = await kills
                .GroupBy(k => k.KillerName)
                .Select(g => new
                {
                    PlayerName = g.Key,
                    KillCount = g.Count(),
                    LastKillDate = g.Max(k => k.CreateDate)
                })
                .ToListAsync();

            // Group by TargetName (victims)
            var victimStats = await kills
                .GroupBy(k => k.TargetName)
                .Select(g => new
                {
                    PlayerName = g.Key,
                    DeathCount = g.Count()
                })
                .ToListAsync();

            // Merge both lists by player name
            var topPlayers = killerStats
                .Select(killer => new PlayerStatsDto
                {
                    PlayerName = killer.PlayerName,
                    KillCount = killer.KillCount,
                    DeathCount = victimStats.FirstOrDefault(v => v.PlayerName == killer.PlayerName)?.DeathCount ?? 0,
                    LastKillDate = killer.LastKillDate
                })
                .OrderByDescending(p => p.KillCount)
                .ThenByDescending(p => p.LastKillDate)
                .Take(topCount)
                .ToList();

            return topPlayers;
        }

        public class PlayerStatsDto
        {
            public string SteamId { get; set; }
            public string PlayerName { get; set; }
            public int KillCount { get; set; }
            public string WeaponName { get; set; }
            public int DeathCount { get; set; }
            public float KillDistance { get; set; }
            public DateTime LastKillDate { get; set; }
        }

    }
}

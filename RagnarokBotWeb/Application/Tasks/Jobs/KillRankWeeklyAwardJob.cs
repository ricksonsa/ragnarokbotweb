using Discord;
using Microsoft.EntityFrameworkCore;
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
        public async Task Execute(long serverId)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(serverId);
                if (!server.RankEnabled) return;
                var topKillersWeekly = await TopPlayers(unitOfWork, server, ERankingPeriod.Weekly);

                var manager = new PlayerCoinManager(unitOfWork);
                await HandleAwards(unitOfWork, discordService, server, topKillersWeekly, manager);
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task HandleAwards(
              IUnitOfWork uow,
              IDiscordService discordService,
              ScumServer server,
              List<PlayerStatsDto> topKillers,
              PlayerCoinManager manager)
        {
            var awards = new[]
            {
                (Rank: 1, Amount: server.KillRankDailyTop1Award),
                (Rank: 2, Amount: server.KillRankDailyTop2Award),
                (Rank: 3, Amount: server.KillRankDailyTop3Award),
                (Rank: 4, Amount: server.KillRankDailyTop4Award),
                (Rank: 5, Amount: server.KillRankDailyTop5Award)
            };

            for (int i = 0; i < topKillers.Count; i++)
            {
                var (rank, amount) = awards[i];
                if (amount.HasValue && amount.Value > 0)
                {
                    var stats = topKillers[i];

                    var player = await uow.Players
                        .Include(p => p.ScumServer)
                        .Include(p => p.Vips)
                        .FirstOrDefaultAsync(p => p.SteamId64 == stats.SteamId && p.ScumServerId == server.Id);

                    if (player != null)
                    {
                        if (server.RankVipOnly && !player.IsVip()) continue;
                        await manager.AddCoinsByPlayerId(player.Id, amount.Value);

                        if (player.DiscordId.HasValue)
                        {
                            var embed = new CreateEmbed(player.DiscordId.Value)
                            {
                                Title = "🏆 Congratulations! 🏆",
                                Text = $"You secured the Top {rank} spot in the Weekly Kill Ranking.\r\n" +
                                       $"As a reward you’ve earned 💰 {amount.Value} Coins! 🔥\r\n\r\n",
                                Color = Color.DarkOrange
                            };
                            await discordService.SendEmbedToUserDM(embed);
                        }
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

            // Merge both lists by player name
            var topPlayers = killerStats
                .Select(killer => new PlayerStatsDto
                {
                    PlayerName = killer.PlayerName,
                    KillCount = killer.KillCount,
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

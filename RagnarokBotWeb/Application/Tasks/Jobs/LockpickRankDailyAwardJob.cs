using Discord;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class LockpickRankDailyAwardJob(
        ILogger<LockpickRankDailyAwardJob> logger,
        IScumServerRepository scumServerRepository,
        IDiscordService discordService,
        IUnitOfWork unitOfWork) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(long serverId)
        {
            logger.LogInformation("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(serverId);
                if (!server.RankEnabled) return;
                var manager = new PlayerCoinManager(unitOfWork);
                var topPlayers = await GetLockpickRank(unitOfWork, server);
                await HandleAwardsDaily(unitOfWork, discordService, server, topPlayers, manager);

            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "LockpickRankJob Exception");
                throw;
            }
        }

        private static async Task HandleAwardsDaily(
          IUnitOfWork uow,
          IDiscordService discordService,
          ScumServer server,
          List<LockpickStatsDto> topLockpickers,
          PlayerCoinManager manager)
        {
            var awards = new[]
            {
                (Rank: 1, Amount: server.LockpickRankDailyTop1Award),
                (Rank: 2, Amount: server.LockpickRankDailyTop2Award),
                (Rank: 3, Amount: server.LockpickRankDailyTop3Award),
                (Rank: 4, Amount: server.LockpickRankDailyTop4Award),
                (Rank: 5, Amount: server.LockpickRankDailyTop5Award)
            };

            for (int i = 0; i < topLockpickers.Count; i++)
            {
                var (rank, amount) = awards[i];
                if (amount.HasValue && amount.Value > 0)
                {
                    var stats = topLockpickers[i];

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
                                Text = $"You secured the Top {rank} spot in the Daily Lockpick Ranking.\r\n" +
                                       $"As a reward you’ve earned 💰 {amount.Value} Coins! 🔥\r\n\r\n",
                                Color = Color.DarkOrange
                            };
                            await discordService.SendEmbedToUserDM(embed);
                        }
                    }
                }
            }
        }

        private static async Task<List<LockpickStatsDto>> GetLockpickRank(IUnitOfWork unitOfWork, ScumServer server)
        {
            var lockpicks = unitOfWork.Lockpicks
                .Include(kill => kill.ScumServer)
                .Where(l => l.ScumServer.Id == server.Id
                         && l.AttemptDate.Date == DateTime.UtcNow.Date);

            var stats = await lockpicks
                .GroupBy(l => new { l.Name, l.SteamId64, l.LockType })
                .Select(g => new LockpickStatsDto
                {
                    PlayerName = g.Key.Name,
                    SteamId = g.Key.SteamId64,
                    LockType = g.Key.LockType,
                    SuccessCount = g.Count(l => l.Success),
                    FailCount = g.Count(l => !l.Success),
                    Attempts = g.Sum(l => l.Attempts),
                    SuccessRate = g.Any()
                        ? (double)g.Count(l => l.Success) / g.Count() * 100
                        : 0
                })
                .OrderBy(p => p.LockType.ToLower() == "advanced" ? 0
                             : p.LockType.ToLower() == "medium" ? 1
                             : 2)
                .ThenByDescending(p => p.SuccessCount)
                .ThenByDescending(p => p.SuccessRate)
                .Take(5)
                .ToListAsync();

            return stats;
        }


        public class LockpickStatsDto
        {
            public string PlayerName { get; set; }
            public string SteamId { get; set; }
            public string LockType { get; set; }
            public int SuccessCount { get; set; }
            public int FailCount { get; set; }
            public int Attempts { get; set; }
            public double SuccessRate { get; set; } // As a percentage
        }

    }
}

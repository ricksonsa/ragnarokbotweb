using Discord;
using Microsoft.EntityFrameworkCore;
using Quartz;
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
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(context);

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
            if (server.LockpickRankDailyTop1Award.HasValue && server.LockpickRankDailyTop1Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topLockpickers[0].PlayerName && p.ScumServerId == server.Id);
                var amount = server.LockpickRankDailyTop1Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 1 spot in the Daily Lockpick Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
            }

            if (server.LockpickRankDailyTop2Award.HasValue && server.LockpickRankDailyTop2Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topLockpickers[0].SteamId && p.ScumServerId == server.Id);
                var amount = server.LockpickRankDailyTop2Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 2 spot in the Daily Lockpick Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
            }

            if (server.LockpickRankDailyTop3Award.HasValue && server.LockpickRankDailyTop3Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topLockpickers[0].SteamId && p.ScumServerId == server.Id);
                var amount = server.LockpickRankDailyTop3Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 3 spot in the Daily Lockpick Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
            }

            if (server.LockpickRankDailyTop4Award.HasValue && server.LockpickRankDailyTop4Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topLockpickers[0].SteamId && p.ScumServerId == server.Id);
                var amount = server.LockpickRankDailyTop4Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 4 spot in the Daily Lockpick Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
            }

            if (server.LockpickRankDailyTop5Award.HasValue && server.LockpickRankDailyTop5Award.Value > 0)
            {
                var player = await uow.Players
                    .Include(p => p.ScumServer)
                    .FirstOrDefaultAsync(p => p.SteamId64 == topLockpickers[0].SteamId && p.ScumServerId == server.Id);
                var amount = server.LockpickRankDailyTop5Award.Value;

                if (player != null)
                {
                    await manager.AddCoinsByPlayerId(player.Id, amount);
                    if (player.DiscordId.HasValue)
                    {
                        var embed = new CreateEmbed(player.DiscordId.Value);
                        embed.Title = "🏆 Congratulations! 🏆";
                        embed.Text = $"You secured the Top 5 spot in the Daily Lockpick Ranking.\r\nAs a reward you’ve earned 💰 {amount} Coins! 🔥\r\n\r\n";
                        embed.Color = Color.DarkOrange;
                        await discordService.SendEmbedToUserDM(embed);
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

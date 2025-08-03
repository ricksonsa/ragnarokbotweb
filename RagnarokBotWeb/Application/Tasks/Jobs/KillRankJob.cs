using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class KillRankJob(
        ILogger<KillRankJob> logger,
        IScumServerRepository scumServerRepository,
        IDiscordService discordService,
        IUnitOfWork unitOfWork,
        IChannelService channelService
        ) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            var server = await GetServerAsync(context);

            var killRankChannel = await channelService.FindByGuildIdAndChannelTypeAsync(server.Guild!.Id, ChannelTemplateValues.KillRank);
            if (killRankChannel is null) return;

            await discordService.DeleteAllMessagesInChannel(killRankChannel.DiscordId);
            await SendTopPlayersMonthly(discordService, unitOfWork, server, killRankChannel);
            await SendTopPlayersWeekly(discordService, unitOfWork, server, killRankChannel);
            await SendTopPlayersDaily(discordService, unitOfWork, server, killRankChannel);

            var sniperRankChannel = await channelService.FindByGuildIdAndChannelTypeAsync(server.Guild!.Id, ChannelTemplateValues.SniperRank);
            if (sniperRankChannel is null) return;

            await discordService.DeleteAllMessagesInChannel(sniperRankChannel.DiscordId);
            await SendTopSnipers(discordService, unitOfWork, server, sniperRankChannel);
        }

        private static async Task SendTopPlayersMonthly(IDiscordService discordService, IUnitOfWork unitOfWork, ScumServer server, Channel channel)
        {
            List<PlayerStatsDto> topPlayers = await TopPlayers(unitOfWork, server, ERankingPeriod.Monthly, 10);
            await discordService.SendTopPlayersKillsEmbed(channel.DiscordId, topPlayers, ERankingPeriod.Monthly, 10);
        }
        private static async Task SendTopPlayersWeekly(IDiscordService discordService, IUnitOfWork unitOfWork, ScumServer server, Channel channel)
        {
            List<PlayerStatsDto> topPlayers = await TopPlayers(unitOfWork, server, ERankingPeriod.Weekly, 10);
            await discordService.SendTopPlayersKillsEmbed(channel.DiscordId, topPlayers, ERankingPeriod.Weekly, 10);
        }

        private static async Task SendTopPlayersDaily(IDiscordService discordService, IUnitOfWork unitOfWork, ScumServer server, Channel channel)
        {
            List<PlayerStatsDto> topPlayers = await TopPlayers(unitOfWork, server, ERankingPeriod.Daily, 10);
            await discordService.SendTopPlayersKillsEmbed(channel.DiscordId, topPlayers, ERankingPeriod.Daily, 10);
        }

        private static async Task SendTopSnipers(IDiscordService discordService, IUnitOfWork unitOfWork, ScumServer server, Channel channel)
        {
            List<PlayerStatsDto> topPlayers = await TopLongestSingleKills(unitOfWork, server, 10);
            await discordService.SendTopDistanceKillsEmbed(channel.DiscordId, topPlayers, 10);
        }

        private static async Task<List<PlayerStatsDto>> TopPlayers(
          IUnitOfWork unitOfWork,
          ScumServer server,
          ERankingPeriod period,
          int topCount = 20)
        {
            // Determine the starting point of the ranking period
            var now = DateTime.UtcNow;
            DateTime periodStart = period switch
            {
                ERankingPeriod.Daily => now.Date,
                ERankingPeriod.Weekly => now.Date.AddDays(-7),
                ERankingPeriod.Monthly => now.Date.AddMonths(-1),
                _ => now.Date
            };

            // Filter kills by server and period
            var kills = unitOfWork.Kills
                .Where(k => k.ScumServer.Id == server.Id && k.CreateDate >= periodStart);

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

        private static async Task<List<PlayerStatsDto>> TopLongestSingleKills(
           IUnitOfWork unitOfWork,
           ScumServer server,
           int topCount = 20)
        {
            var kills = unitOfWork.Kills
                .Where(k => k.ScumServer.Id == server.Id && k.Distance > 0);

            var longestPerPlayer = await kills
                .GroupBy(k => k.KillerName)
                .Select(g => g
                    .OrderByDescending(k => k.Distance)
                    .Select(k => new PlayerStatsDto
                    {
                        PlayerName = k.KillerName,
                        KillDistance = k.Distance!.Value,
                        WeaponName = k.DisplayWeapon
                    })
                    .FirstOrDefault())
                .ToListAsync();

            var top20 = longestPerPlayer
                .OrderByDescending(p => p.KillDistance)
                .Take(topCount)
                .ToList();

            return top20;
        }


        public class PlayerStatsDto
        {
            public string PlayerName { get; set; }
            public int KillCount { get; set; }
            public string WeaponName { get; set; }
            public int DeathCount { get; set; }
            public float KillDistance { get; set; }
            public DateTime LastKillDate { get; set; }
        }

    }
}

using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
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
        public async Task Execute(long serverId)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(serverId);

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
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception)
            {
                throw;
            }
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
          int topCount = 20)
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
                    && k.KillerSteamId64 != "NPC"
                    && !k.IsSameSquad);

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
                .Include(kill => kill.ScumServer)
                .Where(k =>
                k.ScumServer.Id == server.Id
                && k.Distance > 0
                && k.KillerSteamId64 != "-1"
                && !k.IsSameSquad
                && k.Weapon != null && !(k.Weapon.ToLower().Contains("trap") || k.Weapon.ToLower().Contains("mine") || k.Weapon.ToLower().Contains("claymore")));

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

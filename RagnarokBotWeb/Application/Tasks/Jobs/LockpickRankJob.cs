using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class LockpickRankJob(
        ILogger<LockpickRankJob> logger,
        IScumServerRepository scumServerRepository,
        IDiscordService discordService,
        IUnitOfWork unitOfWork,
        IChannelService channelService
        ) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(context);

                var channel = await channelService.FindByGuildIdAndChannelTypeAsync(server.Guild!.Id, ChannelTemplateValues.LockPickRank);
                if (channel is null) return;

                await discordService.DeleteAllMessagesInChannel(channel.DiscordId);

                var killBoxRank = await GetLockpickRank(unitOfWork, server, "KillBox");
                await discordService.SendLockpickRankEmbed(channel.DiscordId, killBoxRank, "Kill Box");

                var dialLockRank = await GetLockpickRank(unitOfWork, server, "DialLock");
                await discordService.SendLockpickRankEmbed(channel.DiscordId, dialLockRank, "Dial Lock");

                var basicRank = await GetLockpickRank(unitOfWork, server, "Basic");
                await discordService.SendLockpickRankEmbed(channel.DiscordId, basicRank, "Iron Lock");

                var mediumRank = await GetLockpickRank(unitOfWork, server, "Medium");
                await discordService.SendLockpickRankEmbed(channel.DiscordId, mediumRank, "Silver Lock");

                var advancedRank = await GetLockpickRank(unitOfWork, server, "Advanced");
                await discordService.SendLockpickRankEmbed(channel.DiscordId, advancedRank, "Gold Lock");
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "LockpickRankJob Exception");
                throw;
            }
        }

        private static async Task<List<LockpickStatsDto>> GetLockpickRank(
            IUnitOfWork unitOfWork,
            ScumServer server,
            string lockType)
        {
            var lockpicks = unitOfWork.Lockpicks
                .Include(kill => kill.ScumServer)
                .Where(l => l.ScumServer.Id == server.Id
                         && l.LockType == lockType
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
            public string LockType { get; set; }
            public int SuccessCount { get; set; }
            public int FailCount { get; set; }
            public int Attempts { get; set; }
            public double SuccessRate { get; set; } // As a percentage
            public string SteamId { get; set; }
        }

    }
}

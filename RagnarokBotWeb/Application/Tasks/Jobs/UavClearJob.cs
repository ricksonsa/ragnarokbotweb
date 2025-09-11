using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class UavClearJob(
        ILogger<UavClearJob> logger,
        IScumServerRepository scumServerRepository,
        IDiscordService discordService
        ) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(long serverId)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

            try
            {
                var server = await GetServerAsync(serverId);
                if (server.Uav?.DiscordChannelId != null)
                {
                    await discordService.DeleteAllMessagesInChannelByDate(ulong.Parse(server.Uav.DiscordChannelId), DateTime.UtcNow.AddMinutes(-15));
                }
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

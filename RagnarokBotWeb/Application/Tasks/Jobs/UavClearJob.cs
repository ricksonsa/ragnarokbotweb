using Quartz;
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
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            try
            {
                var server = await GetServerAsync(context);
                if (server.Uav?.DiscordMessageId != null)
                {
                    await discordService.DeleteAllMessagesInChannelByDate(server.Uav.DiscordMessageId.Value, DateTime.UtcNow.AddMinutes(-15));
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

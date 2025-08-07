using Quartz;
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
            var server = await GetServerAsync(context);

            try
            {
                if (server.Uav?.DiscordId != null)
                {
                    await discordService.DeleteAllMessagesInChannelByDate(server.Uav.DiscordId.Value, DateTime.UtcNow.AddMinutes(-15));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

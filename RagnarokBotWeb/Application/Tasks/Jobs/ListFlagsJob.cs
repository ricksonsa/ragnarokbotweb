using Quartz;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class ListFlagsJob(
     ILogger<ListFlagsJob> logger,
     IBotService botService,
     IScumServerRepository scumServerRepository) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(context, ftpRequired: false, validateSubscription: true);
                long serverId = server.Id;

                if (!botService.IsBotOnline(serverId)) return;
                await botService.SendCommand(serverId, new BotCommand().ListFlags());
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }
    }
}

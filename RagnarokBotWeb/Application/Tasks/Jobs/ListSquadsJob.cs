using Quartz;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class ListSquadsJob(
     ICacheService cacheService,
     ILogger<ListSquadsJob> logger,
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

                if (!cacheService.GetCommandQueue(serverId).Any(command => command.Values.Any(cv => cv.Type == Shared.Enums.ECommandType.SimpleDelivery)))
                {
                    var command = new BotCommand();
                    command.ListSquads();
                    cacheService.EnqueueCommand(serverId, command);
                }
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

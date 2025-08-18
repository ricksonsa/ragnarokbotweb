using Quartz;
using RagnarokBotWeb.Application.BotServer;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class ListSquadsJob(
     ILogger<ListSquadsJob> logger,
     BotSocketServer botSocket,
     IScumServerRepository scumServerRepository) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(context, ftpRequired: false, validateSubscription: true);
                long serverId = server.Id;

                if (!botSocket.IsBotConnected(serverId)) return;
                if (!botSocket.IsBotConnected(serverId)) return;
                await botSocket.SendCommandAsync(serverId, new BotCommand().ListFlags());
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

using Quartz;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;


public class CommandQueueProcessorJob(
    ILogger<CommandQueueProcessorJob> logger,
    IScumServerRepository scumServerRepository,
    ICacheService cacheService,
    IBotService botService
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

        try
        {
            if (!botService.IsBotOnline()) return;
            var server = await GetServerAsync(context, ftpRequired: false, validateSubscription: true);
            if (cacheService.TryDequeueCommand(server.Id, out var command))
            {
                if (command is null) throw new ArgumentNullException("command");
                await botService.SendCommand(server.Id, command);
            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Job} Exception", context.JobDetail.Key.Name);
            throw;
        }
    }
}
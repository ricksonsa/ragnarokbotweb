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
    public async Task Execute(long serverId)
    {
        logger.LogInformation("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

        try
        {
            var server = await GetServerAsync(serverId, ftpRequired: false, validateSubscription: true);
            if (!botService.IsBotOnline(server.Id)) return;
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
            logger.LogError(ex, "{Job} Exception", $"{GetType().Name}({serverId})");
            throw;
        }
    }
}
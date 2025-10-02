using Hangfire;
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
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task Execute(long serverId)
    {
        logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

        try
        {
            var server = await GetServerAsync(serverId, ftpRequired: false, validateSubscription: true);
            if (!botService.IsBotOnline(server.Id)) return;
            if (cacheService.TryDequeueCommand(server.Id, out var command))
            {
                logger.LogInformation("Retrieved command from command queue for server {Server}", server.Id);
                if (command is null) throw new ArgumentNullException("command");
                await botService.SendCommand(server.Id, command);
            }
        }
        catch (ServerUncompliantException) { }
        catch (TenantDisabledException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Job} Exception", $"{GetType().Name}({serverId})");
            throw;
        }
    }
}
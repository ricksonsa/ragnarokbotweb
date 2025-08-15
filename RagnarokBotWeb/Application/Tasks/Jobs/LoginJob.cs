using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class LoginJob(
    ILogger<LoginJob> logger,
    IScumServerRepository scumServerRepository,
    IPlayerService playerService,
    IUnitOfWork uow,
    IFtpService ftpService
    ) : AbstractJob(scumServerRepository), IJob
{

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
        try
        {
            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(server, uow);
            await foreach (var line in processor.UnreadFileLinesAsync(fileType, ftpService, context.CancellationToken))
            {
                if (string.IsNullOrEmpty(line)) continue;

                var (steamId64, scumId, name, loggedIn) = new LoginLogParser().Parse(line);
                if (string.IsNullOrWhiteSpace(steamId64)) continue;

                if (loggedIn)
                    await playerService.PlayerConnected(server, steamId64, scumId, name);
                else
                    playerService.PlayerDisconnected(server.Id, steamId64);
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
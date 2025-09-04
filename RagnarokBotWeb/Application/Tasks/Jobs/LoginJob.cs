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

                var (Date, IpAddress, SteamId, PlayerName, ScumId, IsLoggedIn, X, Y, Z) = new LoginLogParser().Parse(line);
                if (string.IsNullOrWhiteSpace(SteamId)) continue;

                if (IsLoggedIn)
                    await playerService.PlayerConnected(server, SteamId, ScumId, PlayerName, X, Y, Z, IpAddress);
                else
                    playerService.PlayerDisconnected(server.Id, SteamId);
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
using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class LoginJob(
    ILogger<LoginJob> logger,
    IScumServerRepository scumServerRepository,
    IPlayerService playerService,
    IReaderPointerRepository readerPointerRepository,
    IPlayerRepository playerRepository,
    IReaderRepository readerRepository,
    IFtpService ftpService
    ) : AbstractJob(scumServerRepository), IJob
{

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogInformation("Triggered LoginJob->Execute at: {time}", DateTimeOffset.Now);
            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(ftpService, server, fileType, readerPointerRepository, scumServerRepository, readerRepository);
            await foreach (var line in processor.UnreadFileLinesAsync())
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
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }
}
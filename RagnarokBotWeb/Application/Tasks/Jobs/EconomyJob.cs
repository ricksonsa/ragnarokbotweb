using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;


public class EconomyJob(
    ILogger<EconomyJob> logger,
    IScumServerRepository scumServerRepository,
    IReaderPointerRepository readerPointerRepository,
    IPlayerService playerService,
    IReaderRepository readerRepository,
    IFtpService ftpService,
    DiscordChannelPublisher publisher
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogInformation("Triggered EconomyJob->Execute at: {time}", DateTimeOffset.Now);

            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(ftpService, server, fileType, readerPointerRepository, scumServerRepository, readerRepository);

            await foreach (var line in processor.UnreadFileLinesAsync())
            {
                if (string.IsNullOrEmpty(line)) continue;
                if (line.Contains("Game version")) continue;

                if (line.Contains("changed their name"))
                {
                    var (steamId64, scumId, changedName) = new ChangeNameLogParser().Parse(line);
                    await playerService.PlayerConnected(server, steamId64, scumId, changedName);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }
}
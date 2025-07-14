using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class GamePlayJob(
    ILogger<GamePlayJob> logger,
    IScumServerRepository scumServerRepository,
    IBunkerService bunkerService,
    ILockpickService lockpickService,
    IReaderPointerRepository readerPointerRepository,
    IPlayerService playerService,
    IReaderRepository readerRepository,
    IFtpService ftpService
) : AbstractJob(scumServerRepository), IJob
{

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogInformation("Triggered FtpJob->Execute at: {time}", DateTimeOffset.Now);
            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(ftpService, server, fileType, readerPointerRepository, scumServerRepository, readerRepository);
            await foreach (var line in processor.UnreadFileLinesAsync())
            {
                if (string.IsNullOrEmpty(line)) continue;
                if (line.Contains("Game version")) continue;

                if (line.Contains("[LogBunkerLock]") && line.Contains(" is "))
                {
                    var (sector, state, time) = new BunkerLogParser().Parse(line);
                    await bunkerService.UpdateBunkerState(server, sector, state, time);
                }

                if (line.Contains("[LogMinigame] [LockpickingMinigame_C]") ||
                    line.Contains("[LogMinigame] [BP_DialLockMinigame_C]"))
                {
                    var lockpick = new LockpickLogParser(server).Parse(line);
                    if (lockpick is null) continue;
                    await lockpickService.AddLockpickAttemptAsync(lockpick);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, this);
        }
    }
}
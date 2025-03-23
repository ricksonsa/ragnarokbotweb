using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class GamePlayJob : AbstractJob, IJob
{
    private readonly IBunkerService _bunkerService;
    private readonly IFtpService _ftpService;
    private readonly ILockpickService _lockpickService;
    private readonly ILogger<GamePlayJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GamePlayJob(
        ILogger<GamePlayJob> logger,
        IFtpService ftpService,
        IScumServerRepository scumServerRepository,
        IBunkerService bunkerService,
        IServiceProvider serviceProvider,
        ILockpickService lockpickService) : base(scumServerRepository)
    {
        _logger = logger;
        _bunkerService = bunkerService;
        _lockpickService = lockpickService;
        _ftpService = ftpService;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Triggered FtpJob->Execute at: {time}", DateTimeOffset.Now);
            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(_serviceProvider, _ftpService, server, fileType);
            var lines = await processor.ProcessUnreadFileLines();

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                if (line.Contains("Game version")) continue;

                if (line.Contains("[LogBunkerLock]") && line.Contains(" is "))
                {
                    var (sector, state, time) = new BunkerLogParser().Parse(line);
                    await _bunkerService.UpdateBunkerState(server, sector, state, time);
                }

                if (line.Contains("[LogMinigame] [LockpickingMinigame_C]") ||
                    line.Contains("[LogMinigame] [BP_DialLockMinigame_C]"))
                {
                    var lockpick = new LockpickLogParser(server).Parse(line);
                    if (lockpick is null) continue;
                    await _lockpickService.AddLockpickAttemptAsync(lockpick);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, this);
        }
    }
}
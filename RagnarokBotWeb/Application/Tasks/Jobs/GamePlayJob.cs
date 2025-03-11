using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class GamePlayJob : FtpJob, IJob
    {
        private readonly ILogger<GamePlayJob> _logger;
        private readonly IBunkerService _bunkerService;
        private readonly ILockpickService _lockpickService;

        public GamePlayJob(
            ILogger<GamePlayJob> logger,
            IFtpService ftpService,
            IReaderRepository readerRepository,
            IScumServerRepository scumServerRepository,
            IBunkerService bunkerService,
            ILockpickService lockpickService) : base(readerRepository, ftpService, scumServerRepository, "gameplay_")
        {
            _logger = logger;
            _bunkerService = bunkerService;
            _lockpickService = lockpickService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Triggered FtpJob->Execute at: {time}", DateTimeOffset.Now);
                var server = await GetServerAsync(context);

                foreach (var fileName in GetLogFiles(server.Ftp!))
                {
                    _logger.LogInformation("FtpJob->Execute Reading file: " + fileName);

                    foreach (var line in await GetUnreadFileLinesAsync(server.Ftp!, fileName))
                    {
                        if (string.IsNullOrEmpty(line.Value)) continue;
                        if (line.Value.Contains("Game version")) continue;

                        if (line.Value.Contains("[LogBunkerLock]") && line.Value.Contains(" is "))
                        {
                            var (sector, state, time) = new BunkerLogParser().Parse(line.Value);
                            await _bunkerService.UpdateBunkerState(sector, state, time);
                        }

                        if (line.Value.Contains("[LogMinigame] [LockpickingMinigame_C]") || line.Value.Contains("[LogMinigame] [BP_DialLockMinigame_C]"))
                        {
                            var lockpick = new LockpickLogParser().Parse(line.Value);
                            if (lockpick is null) continue;
                            lockpick.ScumServer = server;
                            await _lockpickService.AddLockpickAttemptAsync(lockpick);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, this);
            }
        }
    }
}

using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class GameplayHostedService : TimedHostedService, IHostedService
    {
        private readonly ILogger<GameplayHostedService> _logger;
        private readonly IServiceProvider _services;

        public GameplayHostedService(
            ILogger<GameplayHostedService> logger,
            IFtpService ftpService,
            IServiceProvider services) : base(services, ftpService.GetClient(), "gameplay_", TimeSpan.FromMinutes(5))
        {
            _logger = logger;
            _services = services;
        }

        public override async Task Process()
        {
            try
            {
                _logger.LogInformation("Triggered GameplayHostedService->Process at: {time}", DateTimeOffset.Now);

                using (var scope = _services.CreateScope())
                {
                    var bunkerService = scope.ServiceProvider.GetRequiredService<IBunkerService>();
                    var lockpickService = scope.ServiceProvider.GetRequiredService<ILockpickService>();

                    foreach (var fileName in GetLogFiles())
                    {
                        _logger.LogInformation("GameplayHostedService->Process Reading file: " + fileName);

                        foreach (var line in GetUnreadFileLines(fileName))
                        {
                            if (string.IsNullOrEmpty(line.Value)) continue;
                            if (line.Value.Contains("Game version")) continue;

                            if (line.Value.Contains("[LogBunkerLock]") && line.Value.Contains(" is "))
                            {
                                var (sector, state, time) = new BunkerLogParser().Parse(line.Value);
                                await bunkerService.UpdateBunkerState(sector, state, time);
                            }

                            if (line.Value.Contains("[LogMinigame] [LockpickingMinigame_C]") || line.Value.Contains("[LogMinigame] [BP_DialLockMinigame_C]"))
                            {
                                var lockpick = new LockpickLogParser().Parse(line.Value);
                                if (lockpick is null) continue;
                                await lockpickService.AddLockpickAttemptAsync(lockpick);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, this);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Timer.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Timer.Stop();
            return Task.CompletedTask;
        }
    }
}

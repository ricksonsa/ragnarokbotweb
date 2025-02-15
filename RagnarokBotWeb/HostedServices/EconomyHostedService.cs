using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class EconomyHostedService : TimedHostedService, IHostedService
    {
        private readonly ILogger<EconomyHostedService> _logger;
        private readonly IServiceProvider _services;

        public EconomyHostedService(
            ILogger<EconomyHostedService> logger,
            IFtpService ftpService,
            IServiceProvider services) : base(services, ftpService.GetClient(), "economy_", TimeSpan.FromMinutes(5))
        {
            _logger = logger;
            _services = services;
        }

        public override async Task Process()
        {
            try
            {
                _logger.LogInformation("Triggered EconomyHostedService->Process at: {time}", DateTimeOffset.Now);

                using (var scope = _services.CreateScope())
                {
                    var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

                    foreach (var fileName in GetLogFiles())
                    {
                        _logger.LogInformation("EconomyHostedService->Process Reading file: " + fileName);

                        foreach (var line in GetUnreadFileLines(fileName))
                        {
                            if (string.IsNullOrEmpty(line.Value)) continue;
                            if (line.Value.Contains("Game version")) continue;

                            if (line.Value.Contains("changed their name"))
                            {
                                var (steamId64, scumId, changedName) = new ChangeNameLogParser().Parse(line.Value);
                                await playerService.PlayerConnected(steamId64, scumId, changedName);
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is starting.");
            Timer.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");
            Timer.Stop();
            return Task.CompletedTask;
        }
    }
}

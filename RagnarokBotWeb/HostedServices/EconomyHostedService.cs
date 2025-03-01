using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class EconomyHostedService : TimedFtpHostedService
    {
        private readonly ILogger<EconomyHostedService> _logger;
        private readonly IServiceProvider _services;

        public EconomyHostedService(
            ILogger<EconomyHostedService> logger,
            IFtpService ftpService,
            IServiceProvider services) : base(services, ftpService, "economy_", TimeSpan.FromMinutes(5))
        {
            _logger = logger;
            _services = services;
        }

        public override async Task Process()
        {
            try
            {
                _logger.LogInformation("Triggered EconomyHostedService->Process at: {time}", DateTimeOffset.Now);

                using var scope = _services.CreateScope();
                var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

                var serverRepository = scope.ServiceProvider.GetRequiredService<IScumServerRepository>();
                var servers = await serverRepository.GetActiveServersWithFtp();

                foreach (var server in servers)
                {
                    foreach (var fileName in GetLogFiles(server.Ftp!))
                    {
                        _logger.LogInformation("EconomyHostedService->Process Reading file: " + fileName);

                        foreach (var line in GetUnreadFileLines(server.Ftp!, fileName))
                        {
                            if (string.IsNullOrEmpty(line.Value)) continue;
                            if (line.Value.Contains("Game version")) continue;

                            if (line.Value.Contains("changed their name"))
                            {
                                var (steamId64, scumId, changedName) = new ChangeNameLogParser().Parse(line.Value);
                                await playerService.PlayerConnected(server, steamId64, scumId, changedName);
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }
    }
}

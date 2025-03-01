using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class LoginHostedService : TimedFtpHostedService
    {
        private readonly ILogger<LoginHostedService> _logger;
        private readonly IServiceProvider _services;

        public LoginHostedService(
            ILogger<LoginHostedService> logger,
            IFtpService ftpService,
            IServiceProvider services) : base(services, ftpService, "login_", TimeSpan.FromSeconds(120))
        {
            _logger = logger;
            _services = services;
        }

        public override async Task Process()
        {
            try
            {
                _logger.LogInformation("Triggered LoginLogTask->Process at: {time}", DateTimeOffset.Now);

                using var scope = _services.CreateScope();
                var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();
                var serverRepository = scope.ServiceProvider.GetRequiredService<IScumServerRepository>();
                var servers = await serverRepository.GetActiveServersWithFtp();

                foreach (var server in servers)
                {
                    foreach (var fileName in GetLogFiles(server.Ftp!))
                    {
                        _logger.LogInformation("LoginLogTask->Process Reading file: " + fileName);

                        foreach (var line in GetUnreadFileLines(server.Ftp!, fileName))
                        {
                            if (string.IsNullOrEmpty(line.Value)) continue;

                            var (steamId64, scumId, name, loggedIn) = new LoginLogParser().Parse(line.Value);
                            if (string.IsNullOrWhiteSpace(steamId64)) continue;

                            if (loggedIn)
                            {
                                await playerService.PlayerConnected(server, steamId64, scumId, name);
                            }
                            else
                            {
                                playerService.PlayerDisconnected(server.Id, steamId64);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}

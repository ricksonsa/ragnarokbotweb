using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;

namespace RagnarokBotWeb.HostedServices
{
    public class LoginHostedService : TimedFtpHostedService
    {
        private readonly ILogger<LoginHostedService> _logger;
        private readonly IServiceProvider _services;

        public LoginHostedService(
            ILogger<LoginHostedService> logger,
            IFtpService ftpService,
            IServiceProvider services) : base(services, ftpService.GetClient(), "login_", TimeSpan.FromSeconds(120))
        {
            _logger = logger;
            _services = services;
        }

        public override async Task Process()
        {
            try
            {
                _logger.LogInformation("Triggered LoginLogTask->Process at: {time}", DateTimeOffset.Now);

                using (var scope = _services.CreateScope())
                {
                    var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

                    foreach (var fileName in GetLogFiles())
                    {
                        _logger.LogInformation("LoginLogTask->Process Reading file: " + fileName);

                        foreach (var line in GetUnreadFileLines(fileName))
                        {
                            if (string.IsNullOrEmpty(line.Value)) continue;

                            var (steamId64, scumId, name, loggedIn) = new LoginLogParser().Parse(line.Value);
                            if (string.IsNullOrWhiteSpace(steamId64)) continue;

                            if (loggedIn)
                            {
                                await playerService.PlayerConnected(steamId64, scumId, name);
                            }
                            else
                            {
                                playerService.PlayerDisconnected(steamId64);
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

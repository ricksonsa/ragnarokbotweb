using RagnarokBotWeb.Application.BotServer;

namespace RagnarokBotWeb.HostedServices
{
    public class BotSocketServerHostedService : BackgroundService
    {
        private readonly ILogger<LoadFtpTaskService> _logger;
        private readonly BotSocketServer _server;

        public BotSocketServerHostedService(
            ILogger<LoadFtpTaskService> logger, BotSocketServer server)
        {
            _logger = logger;
            _server = server;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Log(LogLevel.Information, "Initializing BotSocketServer");
            _ = Task.Run(() => _server.StartAsync(stoppingToken), stoppingToken);
            return Task.CompletedTask;
        }
    }
}

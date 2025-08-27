using RagnarokBotWeb.Application.BotServer;

namespace RagnarokBotWeb.HostedServices
{
    public class BotSocketServerHostedService : IHostedService
    {
        private readonly BotSocketServer _botSocketServer;
        private readonly ILogger<BotSocketServerHostedService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        private Task? _serverTask;
        private CancellationTokenSource? _cts;

        public BotSocketServerHostedService(
            BotSocketServer botSocketServer,
            ILogger<BotSocketServerHostedService> logger,
            IHostApplicationLifetime appLifetime)
        {
            _botSocketServer = botSocketServer;
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting BotSocketServer...");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start the socket server in background
            _serverTask = _botSocketServer.StartAsync(_cts.Token);

            // Register shutdown hook for saving state
            _appLifetime.ApplicationStopping.Register(OnStopping);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping BotSocketServer...");

            try
            {
                // Signal cancellation to the server loop
                _cts?.Cancel();

                // Wait for server loop to finish (with shutdown timeout)
                if (_serverTask != null)
                {
                    await Task.WhenAny(_serverTask, Task.Delay(TimeSpan.FromSeconds(10), cancellationToken));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping BotSocketServer");
            }

            _logger.LogInformation("BotSocketServer stopped");
        }

        private void OnStopping()
        {
            _logger.LogInformation("Application is stopping, saving bot state...");
            try
            {
                _botSocketServer.SaveBotStateOnShutdown();
                _logger.LogInformation("Bot state saved successfully during shutdown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save bot state during shutdown");
            }
        }
    }
}

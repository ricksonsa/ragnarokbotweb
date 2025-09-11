using RagnarokBotWeb.Application.Tasks.Jobs;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.BackgroundServices
{
    public class GameplayJobRunnerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameplayJobRunnerService> _logger;

        public GameplayJobRunnerService(IServiceProvider serviceProvider, ILogger<GameplayJobRunnerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GameplayJobRunnerService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var serverRepository = scope.ServiceProvider.GetRequiredService<IScumServerRepository>();
                    var servers = await serverRepository.FindActive();

                    foreach (var server in servers)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        _ = Task.Run(async () =>
                        {
                            using var jobScope = _serviceProvider.CreateScope();
                            var job = jobScope.ServiceProvider.GetRequiredService<GamePlayJob>();

                            try
                            {
                                await job.Execute(server.Id, Domain.Enums.EFileType.Gameplay);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while executing GamePlayJob for server {ServerId}", server.Id);
                            }
                        }, stoppingToken);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while running jobs");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}

using RagnarokBotWeb.Application.Tasks.Jobs;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.BackgroundServices
{
    public class OrderCommandJobRunnerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderCommandJobRunnerService> _logger;

        public OrderCommandJobRunnerService(IServiceProvider serviceProvider, ILogger<OrderCommandJobRunnerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderCommandJobRunnerService started.");

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
                            var job = jobScope.ServiceProvider.GetRequiredService<OrderCommandJob>();

                            try
                            {
                                await job.Execute(server.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while executing OrderCommandJob for server {ServerId}", server.Id);
                            }
                        }, stoppingToken);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while running jobs");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}

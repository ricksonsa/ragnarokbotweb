using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class LoadRaidTimesHostedService : BackgroundService
    {
        private readonly ILogger<LoadRaidTimesHostedService> _logger;
        private readonly IServiceProvider _services;

        public LoadRaidTimesHostedService(
            ILogger<LoadRaidTimesHostedService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _services.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
            await taskService.LoadRaidTimes(stoppingToken);
        }
    }
}

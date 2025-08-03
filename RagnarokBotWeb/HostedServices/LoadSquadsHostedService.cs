using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class LoadSquadsHostedService : BackgroundService
    {
        private readonly ILogger<LoadSquadsHostedService> _logger;
        private readonly IServiceProvider _services;

        public LoadSquadsHostedService(
            ILogger<LoadSquadsHostedService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _services.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
            await taskService.LoadSquads(stoppingToken);
        }
    }
}

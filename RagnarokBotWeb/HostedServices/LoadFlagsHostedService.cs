using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class LoadFlagsHostedService : BackgroundService
    {
        private readonly ILogger<LoadFlagsHostedService> _logger;
        private readonly IServiceProvider _services;

        public LoadFlagsHostedService(
            ILogger<LoadFlagsHostedService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _services.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
            await taskService.LoadFlags(stoppingToken);
        }
    }
}

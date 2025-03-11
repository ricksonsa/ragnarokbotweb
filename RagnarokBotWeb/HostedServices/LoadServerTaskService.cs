using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class LoadServerTaskService : BackgroundService
    {
        private readonly ILogger<LoadServerTaskService> _logger;
        private readonly IServiceProvider _services;

        public LoadServerTaskService(
            ILogger<LoadServerTaskService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Log(LogLevel.Information, "Loading Ftp Tasks");
            using var scope = _services.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
            await taskService.LoadServerTasks(stoppingToken);
        }
    }
}

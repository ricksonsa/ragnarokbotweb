using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class LoadCustomTasksHostedService : BackgroundService
    {
        private readonly ILogger<LoadCustomTasksHostedService> _logger;
        private readonly IServiceProvider _services;

        public LoadCustomTasksHostedService(
            ILogger<LoadCustomTasksHostedService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Log(LogLevel.Information, "Loading Custom Tasks");
            using var scope = _services.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
            await taskService.LoadCustomServersTasks(stoppingToken);
        }
    }
}

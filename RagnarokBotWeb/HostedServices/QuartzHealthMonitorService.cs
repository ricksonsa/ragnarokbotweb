using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    /// <summary>
    /// Health monitoring service that periodically checks Quartz scheduler health
    /// and attempts recovery if issues are detected
    /// </summary>
    public class QuartzHealthMonitorService : BackgroundService
    {
        private readonly ILogger<QuartzHealthMonitorService> _logger;
        private readonly IServiceProvider _services;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes
        private DateTime _lastHealthyCheck = DateTime.UtcNow;

        public QuartzHealthMonitorService(
            ILogger<QuartzHealthMonitorService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Quartz Health Monitor Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                    await PerformHealthCheck(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Quartz Health Monitor Service cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Quartz Health Monitor Service");
                }
            }

            _logger.LogInformation("Quartz Health Monitor Service stopped");
        }

        private async Task PerformHealthCheck(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _services.CreateScope();
                var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

                // Check if scheduler is healthy
                var isHealthy = taskService.IsSchedulerHealthy();

                if (isHealthy)
                {
                    _lastHealthyCheck = DateTime.UtcNow;

                    // Log statistics periodically
                    var stats = taskService.GetJobStatistics();
                    _logger.LogInformation("Scheduler is healthy. Jobs executed: {JobsExecuted}, Running since: {RunningSince}",
                        stats.GetValueOrDefault("NumberOfJobsExecuted", "N/A"),
                        stats.GetValueOrDefault("RunningSince", "N/A"));
                }
                else
                {
                    var timeSinceLastHealthy = DateTime.UtcNow - _lastHealthyCheck;
                    _logger.LogWarning("Scheduler appears unhealthy. Time since last healthy check: {TimeSinceHealthy}",
                        timeSinceLastHealthy);

                    // If unhealthy for more than 10 minutes, attempt recovery
                    if (timeSinceLastHealthy > TimeSpan.FromMinutes(10))
                    {
                        _logger.LogError("Scheduler has been unhealthy for {TimeSinceHealthy}. Attempting recovery...",
                            timeSinceLastHealthy);
                        await AttemptRecovery(cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform health check");
            }
        }

        private async Task AttemptRecovery(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting scheduler recovery process...");

                using var scope = _services.CreateScope();
                var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

                // Reload all tasks to recover from potential issues
                await taskService.LoadAllServersTasks(cancellationToken);
                await taskService.LoadFtpAllServersTasks(cancellationToken);
                await taskService.LoadCustomServersTasks(cancellationToken);

                _logger.LogInformation("Scheduler recovery process completed");
                _lastHealthyCheck = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover scheduler");
            }
        }
    }
}

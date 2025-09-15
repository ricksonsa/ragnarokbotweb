using RagnarokBotWeb.Application.Tasks.Jobs;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Application.Tasks.BackgroundServices
{
    public class PaydayJobRunnerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaydayJobRunnerService> _logger;
        private readonly ConcurrentDictionary<long, long> _scheduledPaydays = new ConcurrentDictionary<long, long>();

        public PaydayJobRunnerService(IServiceProvider serviceProvider, ILogger<PaydayJobRunnerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaydayJobRunnerService started.");

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

                        if (server.CoinAwardIntervalMinutes <= 0) continue;

                        _ = Task.Run(async () =>
                        {
                            if (_scheduledPaydays.TryAdd(server.Id, server.CoinAwardIntervalMinutes))
                            {
                                await Task.Delay(TimeSpan.FromMinutes(server.CoinAwardIntervalMinutes), stoppingToken);
                                using var jobScope = _serviceProvider.CreateScope();
                                var job = jobScope.ServiceProvider.GetRequiredService<PaydayJob>();

                                try
                                {
                                    await job.Execute(server.Id);
                                }
                                catch (OperationCanceledException) { }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error while executing ChatJob for server {ServerId}", server.Id);
                                }
                                finally
                                {
                                    _scheduledPaydays.TryRemove(server.Id, out _);
                                }
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

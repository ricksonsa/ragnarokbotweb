using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class KillHostedService : TimedHostedService, IHostedService
    {
        private readonly ILogger<KillHostedService> _logger;
        private readonly IServiceProvider _services;

        public KillHostedService(
            ILogger<KillHostedService> logger,
            IFtpService ftpService,
            IServiceProvider services) : base(services, ftpService.GetClient(), "kill_", TimeSpan.FromMinutes(5))
        {
            _logger = logger;
            _services = services;
        }

        public override async Task Process()
        {
            try
            {
                _logger.LogInformation("Triggered KillHostedService->Process at: {time}", DateTimeOffset.Now);

                using (var scope = _services.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    foreach (var fileName in GetLogFiles())
                    {
                        _logger.LogInformation("KillHostedService->Process Reading file: " + fileName);

                        var resolvedLines = GetUnreadFileLines(fileName).Where(l => !l.Value.Contains("Game version") || !string.IsNullOrWhiteSpace(l.Value));

                        using (var enumerator = resolvedLines.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                var first = enumerator.Current;
                                if (!enumerator.MoveNext()) break; // Ensure we don't get an incomplete pair
                                var second = enumerator.Current;

                                var kill = new KillLogParser(uow.Users).Parse(first.Value, second.Value);
                                await uow.Kills.AddAsync(kill);
                                await uow.SaveAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is starting.");
            Timer.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");
            Timer.Stop();
            return Task.CompletedTask;
        }

    }
}

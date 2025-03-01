using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class KillHostedService : TimedFtpHostedService
    {
        private readonly ILogger<KillHostedService> _logger;
        private readonly IServiceProvider _services;

        public KillHostedService(
            ILogger<KillHostedService> logger,
            IFtpService ftpService,
            IServiceProvider services) : base(services, ftpService, "kill_", TimeSpan.FromMinutes(5))
        {
            _logger = logger;
            _services = services;
        }

        public override async Task Process()
        {
            try
            {
                _logger.LogInformation("Triggered KillHostedService->Process at: {time}", DateTimeOffset.Now);

                using var scope = _services.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var serverRepository = scope.ServiceProvider.GetRequiredService<IScumServerRepository>();
                var servers = await serverRepository.GetActiveServersWithFtp();

                foreach (var server in servers)
                {
                    foreach (var fileName in GetLogFiles(server.Ftp!))
                    {
                        _logger.LogInformation("KillHostedService->Process Reading file: " + fileName);
                        var resolvedLines = GetUnreadFileLines(server.Ftp!, fileName).Where(l => !l.Value.Contains("Game version") || !string.IsNullOrWhiteSpace(l.Value));

                        using (var enumerator = resolvedLines.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                var first = enumerator.Current;
                                if (!enumerator.MoveNext()) break;
                                var second = enumerator.Current;

                                var kill = new KillLogParser(uow.Players).Parse(first.Value, second.Value);
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
    }
}

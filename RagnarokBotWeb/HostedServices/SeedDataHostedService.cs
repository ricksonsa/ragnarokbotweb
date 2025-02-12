
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.HostedServices
{
    public class SeedDataHostedService : IHostedService
    {
        private readonly ILogger<SeedDataHostedService> _logger;
        private readonly IServiceProvider _services;

        public SeedDataHostedService(
            ILogger<SeedDataHostedService> logger,
            IFtpService ftpService,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _services.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                if (!uow.Bunkers.Any())
                {
                    _logger.Log(LogLevel.Information, "Seeding bunkers data");
                    uow.Bunkers.Add(new Bunker("B3"));
                    uow.Bunkers.Add(new Bunker("Z2"));
                    uow.Bunkers.Add(new Bunker("B2"));
                    uow.Bunkers.Add(new Bunker("B0"));
                    uow.Bunkers.Add(new Bunker("A4"));
                    uow.Bunkers.Add(new Bunker("B1"));
                    uow.Bunkers.Add(new Bunker("A3"));
                    uow.Bunkers.Add(new Bunker("C4"));
                    uow.Bunkers.Add(new Bunker("C1"));
                    uow.Bunkers.Add(new Bunker("C3"));
                    uow.Bunkers.Add(new Bunker("D4"));
                    uow.Bunkers.Add(new Bunker("D2"));
                    uow.Bunkers.Add(new Bunker("A1"));
                    uow.Bunkers.Add(new Bunker("D1"));
                    uow.Bunkers.Add(new Bunker("Z3"));
                    uow.Bunkers.Add(new Bunker("Z1"));
                    uow.Bunkers.Add(new Bunker("C0"));
                    await uow.SaveAsync();
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

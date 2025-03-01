using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;

namespace RagnarokBotWeb.HostedServices
{
    public class ListPlayersHostedService : TimedHostedService
    {
        private readonly ILogger<ListPlayersHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICacheService _cacheService;

        public ListPlayersHostedService(
            IServiceProvider serviceProvider,
            ICacheService cacheService,
            ILogger<ListPlayersHostedService> logger) : base(TimeSpan.FromSeconds(80))
        {
            _serviceProvider = serviceProvider;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async override Task Process()
        {
            _logger.LogInformation("Triggered ListPlayersHostedService->Process at: {time}", DateTimeOffset.Now);

            using (var scope = _serviceProvider.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var bots = await uow.Bots
                    .Include(bot => bot.ScumServer)
                    .Where(bot => bot.State == EBotState.Online)
                    .ToListAsync();

                foreach (var bot in bots)
                {
                    _cacheService.GetCommandQueue(bot.ScumServer.Id).Enqueue(BotCommand.ListPlayers());
                }
            }
        }
    }
}

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

        public ListPlayersHostedService(IServiceProvider serviceProvider, ICacheService cacheService, ILogger<ListPlayersHostedService> logger) : base(TimeSpan.FromSeconds(20))
        {
            _serviceProvider = serviceProvider;
            _cacheService = cacheService;
            _logger = logger;
        }

        public override Task Process()
        {
            _logger.LogInformation("Triggered ListPlayersHostedService->Process at: {time}", DateTimeOffset.Now);

            using (var scope = _serviceProvider.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                if (uow.Bots.Any(bot => bot.State == EBotState.Online))
                {
                    _cacheService.GetCommandQueue().Enqueue(BotCommand.ListPlayers());
                }
            }
            return Task.CompletedTask;
        }
    }
}
